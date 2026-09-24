namespace OpenChatAgents.Domain.Models;

public class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string Provider { get; set; } = LlmProvider.Ollama;
    public required string LlmModel { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int? MaxTokens { get; set; }
    public required string SystemPrompt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<AgentKnowledgeBase> KnowledgeBaseLinks { get; set; } = [];
    public ICollection<AgentMcpServer> McpServerLinks { get; set; } = [];
    public ICollection<AgentSkill> SkillLinks { get; set; } = [];

    /// <summary>Other agents this agent can consult as a tool (orchestrator pattern).</summary>
    public ICollection<AgentSubAgent> SubAgentLinks { get; set; } = [];
}
