using System.ComponentModel.DataAnnotations;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Dtos;

public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static MessageResponse FromEntity(Models.Message message) => new()
    {
        Id = message.Id,
        SessionId = message.SessionId,
        Role = message.Role,
        Content = message.Content,
        ImageUrl = message.ImageObjectKey is null ? null : $"/chat/{message.SessionId}/messages/{message.Id}/image",
        CreatedAt = message.CreatedAt,
    };
}

public class ChatRequest
{
    public Guid SessionId { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>Raw base64 image bytes (no data-URL prefix), optional — only when the agent's model supports vision.</summary>
    public string? ImageBase64 { get; set; }

    public string? ImageContentType { get; set; }
}

public class ChatHistoryResponse
{
    public Guid SessionId { get; set; }
    public List<MessageResponse> Messages { get; set; } = [];
}

public class ModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public long? Size { get; set; }
}

public class ModelsResponse
{
    public List<ModelInfo> Models { get; set; } = [];
}
