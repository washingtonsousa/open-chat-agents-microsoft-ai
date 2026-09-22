using System.ComponentModel.DataAnnotations;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Dtos;

public class ConsumerApplicationCreate
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}

/// <summary>Returned only once, right after creation — the plaintext secret is never retrievable again.</summary>
public class ConsumerApplicationCreated
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class ConsumerApplicationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public UserResponse? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static ConsumerApplicationResponse FromEntity(Models.ConsumerApplication app) => new()
    {
        Id = app.Id,
        Name = app.Name,
        ClientId = app.ClientId,
        IsActive = app.IsActive,
        CreatedBy = app.CreatedByUser is null ? null : UserResponse.FromEntity(app.CreatedByUser),
        CreatedAt = app.CreatedAt,
    };
}

public class ConsumerApplicationListResponse
{
    public List<ConsumerApplicationResponse> ConsumerApplications { get; set; } = [];
    public int Total { get; set; }
}
