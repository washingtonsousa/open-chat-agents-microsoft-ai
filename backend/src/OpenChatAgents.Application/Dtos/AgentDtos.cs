using System.ComponentModel.DataAnnotations;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Dtos;

public class AgentCreate
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [RegularExpression("^(ollama|bedrock)$")]
    public string Provider { get; set; } = "ollama";

    [Required, StringLength(100, MinimumLength = 1)]
    public string LlmModel { get; set; } = string.Empty;

    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.7;

    [Range(1, int.MaxValue)]
    public int? MaxTokens { get; set; }

    [Required, MinLength(1)]
    public string SystemPrompt { get; set; } = string.Empty;

    public Guid[] KnowledgeBaseIds { get; set; } = [];

    public Guid[] McpServerIds { get; set; } = [];
}

public class AgentUpdate
{
    [StringLength(255, MinimumLength = 1)]
    public string? Name { get; set; }

    [RegularExpression("^(ollama|bedrock)$")]
    public string? Provider { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? LlmModel { get; set; }

    [Range(0.0, 2.0)]
    public double? Temperature { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxTokens { get; set; }

    [MinLength(1)]
    public string? SystemPrompt { get; set; }

    public Guid[]? KnowledgeBaseIds { get; set; }

    public Guid[]? McpServerIds { get; set; }
}

public class AgentKnowledgeBaseSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AgentMcpServerSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AgentResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string LlmModel { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public UserResponse? CreatedBy { get; set; }
    public List<AgentKnowledgeBaseSummary> KnowledgeBases { get; set; } = [];
    public List<AgentMcpServerSummary> McpServers { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static AgentResponse FromEntity(Models.Agent agent) => new()
    {
        Id = agent.Id,
        Name = agent.Name,
        Provider = agent.Provider,
        LlmModel = agent.LlmModel,
        Temperature = agent.Temperature,
        MaxTokens = agent.MaxTokens,
        SystemPrompt = agent.SystemPrompt,
        CreatedBy = agent.CreatedByUser is null ? null : UserResponse.FromEntity(agent.CreatedByUser),
        KnowledgeBases = [.. agent.KnowledgeBaseLinks
            .Where(l => l.KnowledgeBase is not null)
            .Select(l => new AgentKnowledgeBaseSummary { Id = l.KnowledgeBase!.Id, Name = l.KnowledgeBase.Name })],
        McpServers = [.. agent.McpServerLinks
            .Where(l => l.McpServer is not null)
            .Select(l => new AgentMcpServerSummary { Id = l.McpServer!.Id, Name = l.McpServer.Name })],
        CreatedAt = agent.CreatedAt,
        UpdatedAt = agent.UpdatedAt,
    };
}

public class AgentListResponse
{
    public List<AgentResponse> Agents { get; set; } = [];
    public int Total { get; set; }
}
