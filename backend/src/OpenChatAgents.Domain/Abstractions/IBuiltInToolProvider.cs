using Microsoft.Extensions.AI;

namespace OpenChatAgents.Domain.Abstractions;

/// <summary>Ambient context a built-in tool needs to act on behalf of the current chat turn.</summary>
public record BuiltInToolContext(Guid UserId);

/// <summary>
/// A native, in-process tool group exposed through the same McpServer/Agent-attachment UI as a
/// real external MCP server (McpServer.Kind == BuiltIn, keyed by BuiltInKey), but resolved
/// in-process instead of over an actual MCP connection.
/// </summary>
public interface IBuiltInToolProvider
{
    /// <summary>Matches McpServer.BuiltInKey.</summary>
    string Key { get; }

    IReadOnlyList<AITool> GetTools(BuiltInToolContext context);
}
