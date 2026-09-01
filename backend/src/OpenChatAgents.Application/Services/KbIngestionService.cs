using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Repositories;
using OpenChatAgents.Domain.Services;
using OpenChatAgents.Domain.Telemetry;
using OpenChatAgents.Domain.VectorStore;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

/// <summary>
/// Caso de uso de ingestão de um documento de KB: extrai texto, faz chunking, gera embeddings
/// e grava na base vetorial. Chamado pelo Worker após um evento de upload no MinIO.
/// </summary>
public class KbIngestionService(
    IKbDocumentRepository documentRepo,
    IObjectStore objectStore,
    ITextExtractor textExtractor,
    IEmbeddingClientFactory embeddingFactory,
    IKbVectorStore vectorStore,
    ILogger<KbIngestionService> logger)
{
    public async Task ProcessDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var activity = AppActivitySource.Source.StartActivity("kb.ingest_document");
        activity?.SetTag("kb.document_id", documentId);

        var document = await documentRepo.GetByIdWithKnowledgeBaseAsync(documentId);
        if (document is null || document.KnowledgeBase is null)
        {
            logger.LogWarning("Documento {DocumentId} não encontrado, ignorando.", documentId);
            return;
        }

        if (document.Status is Models.KbDocumentStatus.Completed or Models.KbDocumentStatus.Processing)
            return;

        logger.LogInformation("Processando documento {DocumentId} ({FileName}) da KB {KnowledgeBaseId}.", document.Id, document.FileName, document.KnowledgeBaseId);
        activity?.SetTag("kb.id", document.KnowledgeBaseId);
        activity?.SetTag("kb.file_name", document.FileName);

        document.Status = Models.KbDocumentStatus.Processing;
        await documentRepo.SaveChangesAsync();

        try
        {
            var chunkCount = await IngestDocumentAsync(document, cancellationToken);

            document.Status = Models.KbDocumentStatus.Completed;
            document.ChunkCount = chunkCount;
            document.ProcessedAt = DateTimeOffset.UtcNow;
            document.ErrorMessage = null;
            activity?.SetTag("kb.chunk_count", chunkCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar documento {DocumentId}.", document.Id);
            document.Status = Models.KbDocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }

        await documentRepo.SaveChangesAsync();
    }

    private async Task<int> IngestDocumentAsync(Models.KbDocument document, CancellationToken cancellationToken)
    {
        var kb = document.KnowledgeBase!;

        await using var stream = await objectStore.GetObjectStreamAsync(document.ObjectKey, cancellationToken);
        var text = textExtractor.Extract(stream, document.FileName);
        var chunks = ChunkingService.Chunk(text, kb.ChunkSize, kb.ChunkOverlap);

        if (chunks.Count == 0)
            return 0;

        var embeddingGenerator = embeddingFactory.Build(kb.EmbeddingProvider, kb.EmbeddingModel);
        var embeddings = await embeddingGenerator.GenerateAsync(
            [.. chunks.Select(c => c.Content)],
            cancellationToken: cancellationToken);

        var records = new List<KbChunkRecord>(chunks.Count);
        var refs = new List<Models.KbChunkRef>(chunks.Count);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunkId = Guid.NewGuid();
            records.Add(new KbChunkRecord(chunkId, document.Id, chunks[i].Index, chunks[i].Content, embeddings[i].Vector));
            refs.Add(new Models.KbChunkRef
            {
                Id = chunkId,
                KbDocumentId = document.Id,
                ChunkIndex = chunks[i].Index,
                CharStart = chunks[i].CharStart,
                CharEnd = chunks[i].CharEnd,
            });
        }

        await vectorStore.UpsertAsync(kb, records, cancellationToken);
        documentRepo.AddChunkRefs(refs);

        return chunks.Count;
    }
}
