using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Infrastructure.Mcp;

public class McpToolFactory(
    ISecretProtector secretProtector,
    IEnumerable<IBuiltInToolProvider> builtInToolProviders,
    ILogger<McpToolFactory> logger) : IMcpToolFactory
{
    public async Task<IMcpToolSession> CreateSessionAsync(IEnumerable<McpServer> servers, BuiltInToolContext context, CancellationToken cancellationToken = default)
    {
        var clients = new List<McpClient>();
        var tools = new List<AITool>();

        foreach (var server in servers)
        {
            if (server.Kind == McpServerKind.BuiltIn)
            {
                var provider = builtInToolProviders.FirstOrDefault(p => p.Key == server.BuiltInKey);
                if (provider is null)
                {
                    logger.LogWarning("Nenhum provedor de ferramenta embutida encontrado para a chave '{Key}'.", server.BuiltInKey);
                    continue;
                }
                tools.AddRange(provider.GetTools(context));
                continue;
            }

            try
            {
                var transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri(server.Url!),
                    AdditionalHeaders = BuildHeaders(server),
                });

                var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
                clients.Add(client);

                var serverTools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                tools.AddRange(serverTools);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Não foi possível conectar ao servidor MCP '{Name}' ({Url}); prosseguindo sem as ferramentas dele.", server.Name, server.Url);
            }
        }

        return new McpToolSession(clients, tools);
    }

    private Dictionary<string, string>? BuildHeaders(McpServer server)
    {
        if (server.AuthType == McpAuthType.BearerToken && !string.IsNullOrEmpty(server.Secret))
        {
            return new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {secretProtector.Unprotect(server.Secret)}",
            };
        }

        if (server.AuthType == McpAuthType.Header && !string.IsNullOrEmpty(server.AuthHeaderName) && !string.IsNullOrEmpty(server.Secret))
        {
            return new Dictionary<string, string>
            {
                [server.AuthHeaderName] = secretProtector.Unprotect(server.Secret),
            };
        }

        return null;
    }

    private sealed class McpToolSession(List<McpClient> clients, List<AITool> tools) : IMcpToolSession
    {
        public IReadOnlyList<AITool> Tools => tools;

        public async ValueTask DisposeAsync()
        {
            foreach (var client in clients)
                await client.DisposeAsync();
        }
    }
}
