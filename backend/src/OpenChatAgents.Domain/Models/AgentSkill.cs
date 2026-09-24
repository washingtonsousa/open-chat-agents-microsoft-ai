namespace OpenChatAgents.Domain.Models;

public class AgentSkill
{
    public Guid AgentId { get; set; }
    public Guid SkillId { get; set; }

    public Agent? Agent { get; set; }
    public Skill? Skill { get; set; }
}
