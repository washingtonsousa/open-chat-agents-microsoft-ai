using System.ComponentModel.DataAnnotations;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Dtos;

public class SkillCreate
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Content { get; set; } = string.Empty;
}

public class SkillUpdate
{
    [StringLength(255, MinimumLength = 1)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [MinLength(1)]
    public string? Content { get; set; }
}

public class SkillResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public UserResponse? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static SkillResponse FromEntity(Models.Skill skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name,
        Description = skill.Description,
        Content = skill.Content,
        CreatedBy = skill.CreatedByUser is null ? null : UserResponse.FromEntity(skill.CreatedByUser),
        CreatedAt = skill.CreatedAt,
        UpdatedAt = skill.UpdatedAt,
    };
}

public class SkillListResponse
{
    public List<SkillResponse> Skills { get; set; } = [];
    public int Total { get; set; }
}
