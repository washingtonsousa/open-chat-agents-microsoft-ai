namespace OpenChatAgents.Domain.Models;

public class KbChunkRef
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KbDocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public int CharStart { get; set; }
    public int CharEnd { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public KbDocument? KbDocument { get; set; }
}
