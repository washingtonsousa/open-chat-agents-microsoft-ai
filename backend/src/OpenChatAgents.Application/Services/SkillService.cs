using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Exceptions;
using OpenChatAgents.Domain.Repositories;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

public class SkillService(ISkillRepository repo)
{
    public async Task<Models.Skill> CreateAsync(SkillCreate payload, Guid createdByUserId)
    {
        var existing = await repo.GetByNameAsync(payload.Name.Trim());
        if (existing is not null)
            throw ApiException.Conflict($"Já existe uma skill com o nome '{payload.Name}'.");

        var skill = new Models.Skill
        {
            Name = payload.Name.Trim(),
            Description = payload.Description,
            Content = payload.Content,
            CreatedByUserId = createdByUserId,
        };

        return await repo.CreateAsync(skill);
    }

    public async Task<Models.Skill> GetAsync(Guid id)
    {
        var skill = await repo.GetByIdAsync(id);
        if (skill is null)
            throw ApiException.NotFound($"Skill {id} não encontrada.");
        return skill;
    }

    public Task<List<Models.Skill>> ListAsync() => repo.ListAllAsync();

    public async Task<Models.Skill> UpdateAsync(Guid id, SkillUpdate payload)
    {
        var skill = await GetAsync(id);
        if (payload.Name is not null && payload.Name != skill.Name)
        {
            var existing = await repo.GetByNameAsync(payload.Name);
            if (existing is not null)
                throw ApiException.Conflict($"Já existe uma skill com o nome '{payload.Name}'.");
        }

        var update = new SkillUpdateFields(payload.Name, payload.Description, payload.Content);
        return await repo.UpdateAsync(skill, update);
    }

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await repo.DeleteAsync(id);
        if (!deleted)
            throw ApiException.NotFound($"Skill {id} não encontrada.");
    }
}
