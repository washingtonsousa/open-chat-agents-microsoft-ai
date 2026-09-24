namespace OpenChatAgents.Domain.Models;

public class Skill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Content { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<AgentSkill> AgentLinks { get; set; } = [];
}
