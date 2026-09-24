using Microsoft.Extensions.AI;
using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Abstractions;

/// <summary>
/// A live set of tools resolved from one or more MCP servers for a single chat turn.
/// Must stay alive (not disposed) for the whole duration of the agent run, since a tool
/// invocation routes back over the same MCP connection — dispose only after streaming ends.
/// </summary>
public interface IMcpToolSession : IAsyncDisposable
{
    IReadOnlyList<AITool> Tools { get; }
}

public interface IMcpToolFactory
{
    /// <summary>
    /// Connects to every given server and lists its tools. A server that fails to connect or
    /// authenticate is skipped (logged, not thrown) so one bad MCP server never breaks the chat.
    /// </summary>
    Task<IMcpToolSession> CreateSessionAsync(IEnumerable<McpServer> servers, BuiltInToolContext context, CancellationToken cancellationToken = default);
}
