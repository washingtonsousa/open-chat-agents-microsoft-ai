using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Repositories;

public class AgentRepository(AppDbContext db)
{
    public async Task<Agent> CreateAsync(string name, string provider, string llmModel, double temperature, int? maxTokens, string systemPrompt, Guid? createdByUserId, IEnumerable<Guid> knowledgeBaseIds)
    {
        var agent = new Agent
        {
            Name = name,
            Provider = provider,
            LlmModel = llmModel,
            Temperature = temperature,
            MaxTokens = maxTokens,
            SystemPrompt = systemPrompt,
            CreatedByUserId = createdByUserId,
        };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        await SyncKnowledgeBasesAsync(agent.Id, knowledgeBaseIds);

        return (await GetByIdAsync(agent.Id))!;
    }

    public Task<Agent?> GetByIdAsync(Guid agentId) =>
        db.Agents
            .Include(a => a.CreatedByUser)
            .Include(a => a.KnowledgeBaseLinks).ThenInclude(l => l.KnowledgeBase)
            .FirstOrDefaultAsync(a => a.Id == agentId);

    public Task<Agent?> GetByNameAsync(string name) =>
        db.Agents.FirstOrDefaultAsync(a => a.Name == name);

    public Task<List<Agent>> ListAllAsync() =>
        db.Agents
            .Include(a => a.CreatedByUser)
            .Include(a => a.KnowledgeBaseLinks).ThenInclude(l => l.KnowledgeBase)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<Agent> UpdateAsync(Agent agent, Dtos.AgentUpdate update)
    {
        if (update.Name is not null) agent.Name = update.Name;
        if (update.Provider is not null) agent.Provider = update.Provider;
        if (update.LlmModel is not null) agent.LlmModel = update.LlmModel;
        if (update.Temperature is not null) agent.Temperature = update.Temperature.Value;
        if (update.MaxTokens is not null) agent.MaxTokens = update.MaxTokens;
        if (update.SystemPrompt is not null) agent.SystemPrompt = update.SystemPrompt;
        agent.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        if (update.KnowledgeBaseIds is not null)
            await SyncKnowledgeBasesAsync(agent.Id, update.KnowledgeBaseIds);

        return (await GetByIdAsync(agent.Id))!;
    }

    public async Task<bool> DeleteAsync(Guid agentId)
    {
        var agent = await GetByIdAsync(agentId);
        if (agent is null) return false;
        db.Agents.Remove(agent);
        await db.SaveChangesAsync();
        return true;
    }

    private async Task SyncKnowledgeBasesAsync(Guid agentId, IEnumerable<Guid> knowledgeBaseIds)
    {
        var existingLinks = await db.AgentKnowledgeBases.Where(l => l.AgentId == agentId).ToListAsync();
        db.AgentKnowledgeBases.RemoveRange(existingLinks);

        foreach (var kbId in knowledgeBaseIds.Distinct())
            db.AgentKnowledgeBases.Add(new AgentKnowledgeBase { AgentId = agentId, KnowledgeBaseId = kbId });

        await db.SaveChangesAsync();
    }
}
