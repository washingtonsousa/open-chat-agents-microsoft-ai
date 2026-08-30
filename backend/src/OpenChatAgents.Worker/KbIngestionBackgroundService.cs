using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenChatAgents.Infrastructure.Agents;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Ingestion;
using OpenChatAgents.Infrastructure.Messaging;
using OpenChatAgents.Infrastructure.Models;
using OpenChatAgents.Infrastructure.Options;
using OpenChatAgents.Infrastructure.Storage;
using OpenChatAgents.Infrastructure.Telemetry;
using OpenChatAgents.Infrastructure.VectorStore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OpenChatAgents.Worker;

public class KbIngestionBackgroundService(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnectionFactory connectionFactory,
    IOptions<AppOptions> options,
    ILogger<KbIngestionBackgroundService> logger) : BackgroundService
{
    private readonly RabbitMqOptions _rabbitOptions = options.Value.RabbitMq;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(_rabbitOptions.ExchangeName, ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(_rabbitOptions.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(_rabbitOptions.QueueName, _rabbitOptions.ExchangeName, routingKey: string.Empty, cancellationToken: stoppingToken);

        logger.LogInformation("Ouvindo fila '{Queue}' vinculada à exchange '{Exchange}'.", _rabbitOptions.QueueName, _rabbitOptions.ExchangeName);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                await HandleNotificationAsync(json, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao processar mensagem de notificação do MinIO.");
            }
            finally
            {
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(_rabbitOptions.QueueName, autoAck: false, consumer, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleNotificationAsync(string json, CancellationToken cancellationToken)
    {
        MinioEventNotification? notification;
        try
        {
            notification = JsonSerializer.Deserialize<MinioEventNotification>(json);
        }
        catch (JsonException)
        {
            logger.LogWarning("Mensagem recebida não é um evento MinIO válido, ignorando.");
            return;
        }

        if (notification is null) return;

        foreach (var record in notification.Records)
        {
            if (!record.EventName.StartsWith("s3:ObjectCreated", StringComparison.OrdinalIgnoreCase))
                continue;

            var key = Uri.UnescapeDataString(record.S3.Object.Key);
            var parts = key.Split('/', 4);
            if (parts.Length < 3 || parts[0] != "kb" || !Guid.TryParse(parts[2], out var documentId))
            {
                logger.LogWarning("Chave de objeto inesperada, ignorando: {Key}", key);
                continue;
            }

            await ProcessDocumentAsync(documentId, cancellationToken);
        }
    }

    private async Task ProcessDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        using var activity = AppActivitySource.Source.StartActivity("kb.ingest_document");
        activity?.SetTag("kb.document_id", documentId);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var objectStore = scope.ServiceProvider.GetRequiredService<MinioObjectStore>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<KbVectorStore>();
        var embeddingFactory = scope.ServiceProvider.GetRequiredService<EmbeddingClientFactory>();

        var document = await db.KbDocuments
            .Include(d => d.KnowledgeBase)
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null || document.KnowledgeBase is null)
        {
            logger.LogWarning("Documento {DocumentId} não encontrado, ignorando.", documentId);
            return;
        }

        if (document.Status is KbDocumentStatus.Completed or KbDocumentStatus.Processing)
            return;

        logger.LogInformation("Processando documento {DocumentId} ({FileName}) da KB {KnowledgeBaseId}.", document.Id, document.FileName, document.KnowledgeBaseId);
        activity?.SetTag("kb.id", document.KnowledgeBaseId);
        activity?.SetTag("kb.file_name", document.FileName);

        document.Status = KbDocumentStatus.Processing;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var chunkCount = await IngestDocumentAsync(document, db, objectStore, vectorStore, embeddingFactory, cancellationToken);

            document.Status = KbDocumentStatus.Completed;
            document.ChunkCount = chunkCount;
            document.ProcessedAt = DateTimeOffset.UtcNow;
            document.ErrorMessage = null;
            activity?.SetTag("kb.chunk_count", chunkCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar documento {DocumentId}.", document.Id);
            document.Status = KbDocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> IngestDocumentAsync(
        KbDocument document,
        AppDbContext db,
        MinioObjectStore objectStore,
        KbVectorStore vectorStore,
        EmbeddingClientFactory embeddingFactory,
        CancellationToken cancellationToken)
    {
        var kb = document.KnowledgeBase!;

        await using var stream = await objectStore.GetObjectStreamAsync(document.ObjectKey, cancellationToken);
        var text = TextExtractor.Extract(stream, document.FileName);
        var chunks = ChunkingService.Chunk(text, kb.ChunkSize, kb.ChunkOverlap);

        if (chunks.Count == 0)
            return 0;

        var embeddingGenerator = embeddingFactory.Build(kb.EmbeddingProvider, kb.EmbeddingModel);
        var embeddings = await embeddingGenerator.GenerateAsync(
            [.. chunks.Select(c => c.Content)],
            cancellationToken: cancellationToken);

        var records = new List<KbChunkRecord>(chunks.Count);
        var refs = new List<KbChunkRef>(chunks.Count);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunkId = Guid.NewGuid();
            records.Add(new KbChunkRecord(chunkId, document.Id, chunks[i].Index, chunks[i].Content, embeddings[i].Vector));
            refs.Add(new KbChunkRef
            {
                Id = chunkId,
                KbDocumentId = document.Id,
                ChunkIndex = chunks[i].Index,
                CharStart = chunks[i].CharStart,
                CharEnd = chunks[i].CharEnd,
            });
        }

        await vectorStore.UpsertAsync(kb, records, cancellationToken);
        db.KbChunkRefs.AddRange(refs);

        return chunks.Count;
    }
}
