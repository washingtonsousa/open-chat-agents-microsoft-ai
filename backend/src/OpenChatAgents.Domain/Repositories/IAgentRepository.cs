using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

/// <summary>Partial-update fields for an Agent; null means "leave unchanged".</summary>
public record AgentUpdateFields(
    string? Name,
    string? Provider,
    string? LlmModel,
    double? Temperature,
    int? MaxTokens,
    string? SystemPrompt,
    Guid[]? KnowledgeBaseIds);

public interface IAgentRepository
{
    Task<Agent> CreateAsync(string name, string provider, string llmModel, double temperature, int? maxTokens, string systemPrompt, Guid? createdByUserId, IEnumerable<Guid> knowledgeBaseIds);
    Task<Agent?> GetByIdAsync(Guid agentId);
    Task<Agent?> GetByNameAsync(string name);
    Task<List<Agent>> ListAllAsync();
    Task<Agent> UpdateAsync(Agent agent, AgentUpdateFields update);
    Task<bool> DeleteAsync(Guid agentId);
}
