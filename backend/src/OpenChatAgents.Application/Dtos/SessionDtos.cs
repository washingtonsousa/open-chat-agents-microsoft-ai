using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Dtos;

public class SessionCreate
{
    public string Title { get; set; } = "Nova conversa";
    public Guid? AgentId { get; set; }
}

public class SessionResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? AgentId { get; set; }
    public AgentResponse? Agent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static SessionResponse FromEntity(Models.Session session) => new()
    {
        Id = session.Id,
        Title = session.Title,
        AgentId = session.AgentId,
        Agent = session.Agent is null ? null : AgentResponse.FromEntity(session.Agent),
        CreatedAt = session.CreatedAt,
        UpdatedAt = session.UpdatedAt,
    };
}

public class SessionListResponse
{
    public List<SessionResponse> Sessions { get; set; } = [];
    public int Total { get; set; }
}
