using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Domain.Models;
using OpenChatAgents.Domain.Repositories;
using OpenChatAgents.Infrastructure.Data;

namespace OpenChatAgents.Infrastructure.Persistence;

public class AgentRepository(AppDbContext db) : IAgentRepository
{
    public async Task<Agent> CreateAsync(string name, string provider, string llmModel, double temperature, int? maxTokens, string systemPrompt, Guid? createdByUserId, IEnumerable<Guid> knowledgeBaseIds, IEnumerable<Guid> mcpServerIds, IEnumerable<Guid> subAgentIds, IEnumerable<Guid> skillIds)
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
        await SyncMcpServersAsync(agent.Id, mcpServerIds);
        await SyncSubAgentsAsync(agent.Id, subAgentIds);
        await SyncSkillsAsync(agent.Id, skillIds);

        return (await GetByIdAsync(agent.Id))!;
    }

    public Task<Agent?> GetByIdAsync(Guid agentId) =>
        db.Agents
            .Include(a => a.CreatedByUser)
            .Include(a => a.KnowledgeBaseLinks).ThenInclude(l => l.KnowledgeBase)
            .Include(a => a.McpServerLinks).ThenInclude(l => l.McpServer)
            .Include(a => a.SubAgentLinks).ThenInclude(l => l.SubAgent)
            .Include(a => a.SkillLinks).ThenInclude(l => l.Skill)
            .FirstOrDefaultAsync(a => a.Id == agentId);

    public Task<Agent?> GetByNameAsync(string name) =>
        db.Agents.FirstOrDefaultAsync(a => a.Name == name);

    public Task<List<Agent>> ListAllAsync() =>
        db.Agents
            .Include(a => a.CreatedByUser)
            .Include(a => a.KnowledgeBaseLinks).ThenInclude(l => l.KnowledgeBase)
            .Include(a => a.McpServerLinks).ThenInclude(l => l.McpServer)
            .Include(a => a.SubAgentLinks).ThenInclude(l => l.SubAgent)
            .Include(a => a.SkillLinks).ThenInclude(l => l.Skill)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<Agent> UpdateAsync(Agent agent, AgentUpdateFields update)
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

        if (update.McpServerIds is not null)
            await SyncMcpServersAsync(agent.Id, update.McpServerIds);

        if (update.SubAgentIds is not null)
            await SyncSubAgentsAsync(agent.Id, update.SubAgentIds);

        if (update.SkillIds is not null)
            await SyncSkillsAsync(agent.Id, update.SkillIds);

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

    private async Task SyncMcpServersAsync(Guid agentId, IEnumerable<Guid> mcpServerIds)
    {
        var existingLinks = await db.AgentMcpServers.Where(l => l.AgentId == agentId).ToListAsync();
        db.AgentMcpServers.RemoveRange(existingLinks);

        foreach (var serverId in mcpServerIds.Distinct())
            db.AgentMcpServers.Add(new AgentMcpServer { AgentId = agentId, McpServerId = serverId });

        await db.SaveChangesAsync();
    }

    private async Task SyncSubAgentsAsync(Guid agentId, IEnumerable<Guid> subAgentIds)
    {
        var existingLinks = await db.AgentSubAgents.Where(l => l.AgentId == agentId).ToListAsync();
        db.AgentSubAgents.RemoveRange(existingLinks);

        foreach (var subAgentId in subAgentIds.Distinct().Where(id => id != agentId))
            db.AgentSubAgents.Add(new AgentSubAgent { AgentId = agentId, SubAgentId = subAgentId });

        await db.SaveChangesAsync();
    }

    private async Task SyncSkillsAsync(Guid agentId, IEnumerable<Guid> skillIds)
    {
        var existingLinks = await db.AgentSkills.Where(l => l.AgentId == agentId).ToListAsync();
        db.AgentSkills.RemoveRange(existingLinks);

        foreach (var skillId in skillIds.Distinct())
            db.AgentSkills.Add(new AgentSkill { AgentId = agentId, SkillId = skillId });

        await db.SaveChangesAsync();
    }
}
