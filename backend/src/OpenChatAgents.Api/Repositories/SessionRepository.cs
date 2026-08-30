using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Repositories;

public class SessionRepository(AppDbContext db)
{
    public async Task<Session> CreateAsync(string title, Guid? agentId)
    {
        var session = new Session { Title = title, AgentId = agentId };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(session.Id))!;
    }

    public Task<Session?> GetByIdAsync(Guid id) =>
        db.Sessions
            .Include(s => s.Agent).ThenInclude(a => a!.KnowledgeBaseLinks).ThenInclude(l => l.KnowledgeBase)
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<Session>> ListAllAsync() =>
        db.Sessions
            .Include(s => s.Agent)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

    public async Task<bool> DeleteAsync(Guid id)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session is null) return false;
        db.Sessions.Remove(session);
        await db.SaveChangesAsync();
        return true;
    }
}
