namespace OpenChatAgents.Api.Models;

public class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string Provider { get; set; } = LlmProvider.Ollama;
    public required string LlmModel { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int? MaxTokens { get; set; }
    public required string SystemPrompt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Session> Sessions { get; set; } = [];
}
