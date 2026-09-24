using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

/// <summary>
/// Partial-update fields for a McpServer; null means "leave unchanged".
/// To clear a stored secret, set AuthType to McpAuthType.None in the same update
/// (the service layer treats that as "no secret needed" regardless of Secret).
/// </summary>
public record McpServerUpdateFields(
    string? Name,
    string? Description,
    string? Url,
    string? AuthType,
    string? AuthHeaderName,
    string? Secret);

public interface IMcpServerRepository
{
    Task<McpServer> CreateAsync(McpServer server);
    Task<McpServer?> GetByIdAsync(Guid id);
    Task<McpServer?> GetByNameAsync(string name);
    Task<List<McpServer>> ListAllAsync();
    Task<McpServer> UpdateAsync(McpServer server, McpServerUpdateFields update);
    Task<bool> DeleteAsync(Guid id);
}
