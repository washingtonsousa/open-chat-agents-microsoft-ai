using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Domain.Models;
using OpenChatAgents.Domain.Repositories;
using OpenChatAgents.Infrastructure.Data;

namespace OpenChatAgents.Infrastructure.Persistence;

public class SkillRepository(AppDbContext db) : ISkillRepository
{
    public async Task<Skill> CreateAsync(Skill skill)
    {
        db.Skills.Add(skill);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(skill.Id))!;
    }

    public Task<Skill?> GetByIdAsync(Guid id) =>
        db.Skills.Include(s => s.CreatedByUser).FirstOrDefaultAsync(s => s.Id == id);

    public Task<Skill?> GetByNameAsync(string name) =>
        db.Skills.FirstOrDefaultAsync(s => s.Name == name);

    public Task<List<Skill>> ListAllAsync() =>
        db.Skills
            .Include(s => s.CreatedByUser)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public async Task<Skill> UpdateAsync(Skill skill, SkillUpdateFields update)
    {
        if (update.Name is not null) skill.Name = update.Name;
        if (update.Description is not null) skill.Description = update.Description;
        if (update.Content is not null) skill.Content = update.Content;
        skill.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return (await GetByIdAsync(skill.Id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var skill = await db.Skills.FirstOrDefaultAsync(s => s.Id == id);
        if (skill is null) return false;
        db.Skills.Remove(skill);
        await db.SaveChangesAsync();
        return true;
    }
}
