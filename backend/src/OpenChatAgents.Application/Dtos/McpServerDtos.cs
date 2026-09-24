using System.ComponentModel.DataAnnotations;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Dtos;

public class McpServerCreate
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required, Url]
    public string Url { get; set; } = string.Empty;

    [RegularExpression("^(none|bearer-token|header)$")]
    public string AuthType { get; set; } = "none";

    public string? AuthHeaderName { get; set; }

    public string? Secret { get; set; }
}

public class McpServerUpdate
{
    [StringLength(255, MinimumLength = 1)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [Url]
    public string? Url { get; set; }

    [RegularExpression("^(none|bearer-token|header)$")]
    public string? AuthType { get; set; }

    public string? AuthHeaderName { get; set; }

    /// <summary>Leave null to keep the current secret unchanged; the write side never round-trips the raw value.</summary>
    public string? Secret { get; set; }
}

public class McpServerResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string AuthType { get; set; } = string.Empty;
    public string? AuthHeaderName { get; set; }
    public bool HasSecret { get; set; }
    public string? BuiltInKey { get; set; }
    public UserResponse? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static McpServerResponse FromEntity(Models.McpServer server) => new()
    {
        Id = server.Id,
        Name = server.Name,
        Description = server.Description,
        Kind = server.Kind,
        Url = server.Url,
        AuthType = server.AuthType,
        AuthHeaderName = server.AuthHeaderName,
        HasSecret = !string.IsNullOrEmpty(server.Secret),
        BuiltInKey = server.BuiltInKey,
        CreatedBy = server.CreatedByUser is null ? null : UserResponse.FromEntity(server.CreatedByUser),
        CreatedAt = server.CreatedAt,
        UpdatedAt = server.UpdatedAt,
    };
}

public class McpServerListResponse
{
    public List<McpServerResponse> McpServers { get; set; } = [];
    public int Total { get; set; }
}
