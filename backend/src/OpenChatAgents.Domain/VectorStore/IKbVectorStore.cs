using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.VectorStore;

public record KbChunkRecord(Guid Id, Guid KbDocumentId, int ChunkIndex, string Content, ReadOnlyMemory<float> Embedding);

public record KbSearchResult(Guid Id, Guid KbDocumentId, int ChunkIndex, string Content, double Score);

public interface IKbVectorStore
{
    Task EnsureCollectionExistsAsync(KnowledgeBase kb, CancellationToken cancellationToken = default);
    Task UpsertAsync(KnowledgeBase kb, IEnumerable<KbChunkRecord> chunks, CancellationToken cancellationToken = default);
    Task<List<KbSearchResult>> SearchAsync(KnowledgeBase kb, ReadOnlyMemory<float> queryEmbedding, int top, CancellationToken cancellationToken = default);
    Task DeleteCollectionAsync(KnowledgeBase kb, CancellationToken cancellationToken = default);
}
