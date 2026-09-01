using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Exceptions;
using OpenChatAgents.Domain.Repositories;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

public class AgentService(IAgentRepository repo)
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

        var update = new AgentUpdateFields(
            payload.Name,
            payload.Provider,
            payload.LlmModel,
            payload.Temperature,
            payload.MaxTokens,
            payload.SystemPrompt,
            payload.KnowledgeBaseIds);

        return await repo.UpdateAsync(agent, update);
    }

    public async Task DeleteAgentAsync(Guid agentId)
    {
        var deleted = await repo.DeleteAsync(agentId);
        if (!deleted)
            throw ApiException.NotFound($"Agente {agentId} não encontrado.");
    }
}
