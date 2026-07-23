namespace OpenChatAgents.Api.Models;

public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "Nova conversa";
    public Guid? AgentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Agent? Agent { get; set; }
    public ICollection<Message> Messages { get; set; } = [];
}
