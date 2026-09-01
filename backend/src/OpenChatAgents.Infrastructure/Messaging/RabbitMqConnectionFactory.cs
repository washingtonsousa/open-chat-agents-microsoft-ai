using Microsoft.Extensions.Options;
using OpenChatAgents.Domain.Options;
using RabbitMQ.Client;

namespace OpenChatAgents.Infrastructure.Messaging;

public class RabbitMqConnectionFactory(IOptions<AppOptions> options)
{
    private readonly RabbitMqOptions _options = options.Value.RabbitMq;

    public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
        };
        return await factory.CreateConnectionAsync(cancellationToken);
    }
}
