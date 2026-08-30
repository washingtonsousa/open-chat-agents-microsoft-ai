using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Repositories;

public class KnowledgeBaseRepository(AppDbContext db)
{
    public async Task<KnowledgeBase> CreateAsync(KnowledgeBase kb)
    {
        db.KnowledgeBases.Add(kb);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(kb.Id))!;
    }

    public Task<KnowledgeBase?> GetByIdAsync(Guid id) =>
        db.KnowledgeBases
            .Include(k => k.CreatedByUser)
            .Include(k => k.Documents)
            .FirstOrDefaultAsync(k => k.Id == id);

    public Task<KnowledgeBase?> GetByNameAsync(string name) =>
        db.KnowledgeBases.FirstOrDefaultAsync(k => k.Name == name);

    public Task<List<KnowledgeBase>> ListAllAsync() =>
        db.KnowledgeBases
            .Include(k => k.CreatedByUser)
            .Include(k => k.Documents)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

    public async Task<bool> DeleteAsync(Guid id)
    {
        var kb = await db.KnowledgeBases.FirstOrDefaultAsync(k => k.Id == id);
        if (kb is null) return false;
        db.KnowledgeBases.Remove(kb);
        await db.SaveChangesAsync();
        return true;
    }
}
