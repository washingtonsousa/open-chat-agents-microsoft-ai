using OpenChatAgents.Api.Dtos;
using OpenChatAgents.Api.Repositories;
using Models = OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Services;

public class AgentService(AgentRepository repo)
{
    public async Task<Models.Agent> CreateAgentAsync(AgentCreate payload, Guid createdByUserId)
    {
        var existing = await repo.GetByNameAsync(payload.Name.Trim());
        if (existing is not null)
            throw ApiException.Conflict($"Já existe um agente com o nome '{payload.Name}'.");

        return await repo.CreateAsync(
            payload.Name.Trim(),
            payload.Provider,
            payload.LlmModel,
            payload.Temperature,
            payload.MaxTokens,
            payload.SystemPrompt,
            createdByUserId,
            payload.KnowledgeBaseIds);
    }

    public async Task<Models.Agent> GetAgentAsync(Guid agentId)
    {
        var agent = await repo.GetByIdAsync(agentId);
        if (agent is null)
            throw ApiException.NotFound($"Agente {agentId} não encontrado.");
        return agent;
    }

    public Task<List<Models.Agent>> ListAgentsAsync() => repo.ListAllAsync();

    public async Task<Models.Agent> UpdateAgentAsync(Guid agentId, AgentUpdate payload)
    {
        var agent = await GetAgentAsync(agentId);
        if (payload.Name is not null && payload.Name != agent.Name)
        {
            var existing = await repo.GetByNameAsync(payload.Name);
            if (existing is not null)
                throw ApiException.Conflict($"Já existe um agente com o nome '{payload.Name}'.");
        }
        return await repo.UpdateAsync(agent, payload);
    }

    public async Task DeleteAgentAsync(Guid agentId)
    {
        var deleted = await repo.DeleteAsync(agentId);
        if (!deleted)
            throw ApiException.NotFound($"Agente {agentId} não encontrado.");
    }
}
