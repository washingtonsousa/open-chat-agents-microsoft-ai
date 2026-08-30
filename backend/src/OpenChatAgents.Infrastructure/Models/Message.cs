namespace OpenChatAgents.Infrastructure.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Session? Session { get; set; }
}
