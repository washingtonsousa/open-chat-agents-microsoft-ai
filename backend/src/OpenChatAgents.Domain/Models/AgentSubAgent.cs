namespace OpenChatAgents.Domain.Models;

/// <summary>Self-referencing join: the parent Agent can consult the child (sub) Agent as a tool.</summary>
public class AgentSubAgent
{
    public Guid AgentId { get; set; }
    public Guid SubAgentId { get; set; }

    public Agent? Agent { get; set; }
    public Agent? SubAgent { get; set; }
}
