namespace OpenChatAgents.Infrastructure.Models;

public class KbDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KnowledgeBaseId { get; set; }
    public required string FileName { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public required string ObjectKey { get; set; }
    public string Status { get; set; } = KbDocumentStatus.Uploaded;
    public string? ErrorMessage { get; set; }
    public int ChunkCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }

    public KnowledgeBase? KnowledgeBase { get; set; }
    public ICollection<KbChunkRef> Chunks { get; set; } = [];
}
