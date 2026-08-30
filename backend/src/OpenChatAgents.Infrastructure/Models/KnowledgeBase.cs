namespace OpenChatAgents.Infrastructure.Models;

public class KnowledgeBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
    public string EmbeddingProvider { get; set; } = LlmProvider.Ollama;
    public required string EmbeddingModel { get; set; }
    public int EmbeddingDimensions { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<KbDocument> Documents { get; set; } = [];
    public ICollection<AgentKnowledgeBase> AgentLinks { get; set; } = [];

    public string WeaviateCollectionName => $"Kb_{Id:N}";
}
