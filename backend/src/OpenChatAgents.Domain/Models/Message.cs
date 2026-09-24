namespace OpenChatAgents.Domain.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public string? ImageObjectKey { get; set; }
    public string? ImageContentType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Session? Session { get; set; }
}
