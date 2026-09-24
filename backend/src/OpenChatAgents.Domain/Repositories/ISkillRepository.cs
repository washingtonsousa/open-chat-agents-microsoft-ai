using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

/// <summary>Partial-update fields for a Skill; null means "leave unchanged".</summary>
public record SkillUpdateFields(string? Name, string? Description, string? Content);

public interface ISkillRepository
{
    Task<Skill> CreateAsync(Skill skill);
    Task<Skill?> GetByIdAsync(Guid id);
    Task<Skill?> GetByNameAsync(string name);
    Task<List<Skill>> ListAllAsync();
    Task<Skill> UpdateAsync(Skill skill, SkillUpdateFields update);
    Task<bool> DeleteAsync(Guid id);
}
