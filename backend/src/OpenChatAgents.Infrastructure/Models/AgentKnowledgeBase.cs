namespace OpenChatAgents.Infrastructure.Models;

public class AgentKnowledgeBase
{
    public Guid AgentId { get; set; }
    public Guid KnowledgeBaseId { get; set; }

    public Agent? Agent { get; set; }
    public KnowledgeBase? KnowledgeBase { get; set; }
}
