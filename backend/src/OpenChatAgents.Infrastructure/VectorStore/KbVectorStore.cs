using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.VectorData;
using OpenChatAgents.Domain.Models;
using OpenChatAgents.Domain.VectorStore;

namespace OpenChatAgents.Infrastructure.VectorStore;

public class KbVectorStore(Microsoft.Extensions.VectorData.VectorStore vectorStore) : IKbVectorStore
{
    private static VectorStoreCollectionDefinition BuildDefinition(int dimensions) => new()
    {
        Properties =
        [
            new VectorStoreKeyProperty("ChunkKey", typeof(Guid)),
            new VectorStoreDataProperty("KbDocumentId", typeof(Guid)) { IsIndexed = true },
            new VectorStoreDataProperty("ChunkIndex", typeof(int)),
            new VectorStoreDataProperty("Content", typeof(string)),
            new VectorStoreVectorProperty("embedding", dimensions)
            {
                DistanceFunction = Microsoft.Extensions.VectorData.DistanceFunction.CosineDistance,
            },
        ],
    };

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Dynamic Dictionary-based records; this app is not published AOT/trimmed.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Dynamic Dictionary-based records; this app is not published AOT/trimmed.")]
    private VectorStoreCollection<Guid, Dictionary<string, object?>> GetCollection(KnowledgeBase kb) =>
        vectorStore.GetCollection<Guid, Dictionary<string, object?>>(kb.WeaviateCollectionName, BuildDefinition(kb.EmbeddingDimensions));

    public async Task EnsureCollectionExistsAsync(KnowledgeBase kb, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(kb);
        await collection.EnsureCollectionExistsAsync(cancellationToken);
    }

    public async Task UpsertAsync(KnowledgeBase kb, IEnumerable<KbChunkRecord> chunks, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(kb);
        await collection.EnsureCollectionExistsAsync(cancellationToken);

        var records = chunks.Select(c => new Dictionary<string, object?>
        {
            ["ChunkKey"] = c.Id,
            ["KbDocumentId"] = c.KbDocumentId,
            ["ChunkIndex"] = c.ChunkIndex,
            ["Content"] = c.Content,
            ["embedding"] = c.Embedding,
        });

        await collection.UpsertAsync(records, cancellationToken);
    }

    public async Task<List<KbSearchResult>> SearchAsync(KnowledgeBase kb, ReadOnlyMemory<float> queryEmbedding, int top, CancellationToken cancellationToken = default)
    {
        if (!await vectorStore.CollectionExistsAsync(kb.WeaviateCollectionName, cancellationToken))
            return [];

        var collection = GetCollection(kb);
        var results = new List<KbSearchResult>();

        await foreach (var result in collection.SearchAsync(queryEmbedding, top, cancellationToken: cancellationToken))
        {
            results.Add(new KbSearchResult(
                (Guid)GetValue(result.Record, "ChunkKey")!,
                (Guid)GetValue(result.Record, "KbDocumentId")!,
                Convert.ToInt32(GetValue(result.Record, "ChunkIndex")),
                (string)GetValue(result.Record, "Content")!,
                result.Score ?? 0));
        }

        return results;
    }

    /// <summary>
    /// Weaviate returns property names with a different casing than declared (e.g. "chunkKey"
    /// instead of "ChunkKey"), so lookups on the search result dictionary are done case-insensitively.
    /// </summary>
    private static object? GetValue(Dictionary<string, object?> record, string name) =>
        record.FirstOrDefault(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

    public async Task DeleteCollectionAsync(KnowledgeBase kb, CancellationToken cancellationToken = default)
    {
        await vectorStore.EnsureCollectionDeletedAsync(kb.WeaviateCollectionName, cancellationToken);
    }
}
