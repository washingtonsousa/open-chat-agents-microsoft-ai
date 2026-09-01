using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenChatAgents.Application.Services;
using OpenChatAgents.Domain.Options;
using OpenChatAgents.Infrastructure.Messaging;
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

            using var scope = scopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<KbIngestionService>();
            await ingestionService.ProcessDocumentAsync(documentId, cancellationToken);
        }
    }
}
