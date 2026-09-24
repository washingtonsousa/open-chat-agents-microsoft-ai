namespace OpenChatAgents.Domain.Models;

public class McpServer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Kind { get; set; } = McpServerKind.External;

    /// <summary>Only meaningful for Kind == External.</summary>
    public string? Url { get; set; }
    public string AuthType { get; set; } = McpAuthType.None;
    public string? AuthHeaderName { get; set; }

    /// <summary>Ciphertext (protected via ISecretProtector) — never the raw token.</summary>
    public string? Secret { get; set; }

    /// <summary>Only meaningful for Kind == BuiltIn — matches IBuiltInToolProvider.Key.</summary>
    public string? BuiltInKey { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<AgentMcpServer> AgentLinks { get; set; } = [];
}
