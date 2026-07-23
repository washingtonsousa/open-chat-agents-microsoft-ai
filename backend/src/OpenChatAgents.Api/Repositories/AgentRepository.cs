using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Api.Data;
using OpenChatAgents.Api.Models;

namespace OpenChatAgents.Api.Repositories;

public class AgentRepository(AppDbContext db)
{
    public async Task<Agent> CreateAsync(string name, string provider, string llmModel, double temperature, int? maxTokens, string systemPrompt)
    {
        var agent = new Agent
        {
            Name = name,
            Provider = provider,
            LlmModel = llmModel,
            Temperature = temperature,
            MaxTokens = maxTokens,
            SystemPrompt = systemPrompt,
        };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();
        return agent;
    }

    public Task<Agent?> GetByIdAsync(Guid id) =>
        db.Agents.FirstOrDefaultAsync(a => a.Id == id);

    public Task<Agent?> GetByNameAsync(string name) =>
        db.Agents.FirstOrDefaultAsync(a => a.Name == name);

    public Task<List<Agent>> ListAllAsync() =>
        db.Agents.OrderByDescending(a => a.CreatedAt).ToListAsync();

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
        return agent;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var agent = await GetByIdAsync(id);
        if (agent is null) return false;
        db.Agents.Remove(agent);
        await db.SaveChangesAsync();
        return true;
    }
}
