using System.ComponentModel.DataAnnotations;
using Models = OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Dtos;

public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public static MessageResponse FromEntity(Models.Message message) => new()
    {
        Id = message.Id,
        SessionId = message.SessionId,
        Role = message.Role,
        Content = message.Content,
        CreatedAt = message.CreatedAt,
    };
}

public class ChatRequest
{
    public Guid SessionId { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;
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
