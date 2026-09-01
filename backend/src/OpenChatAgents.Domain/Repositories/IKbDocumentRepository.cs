using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

public interface IKbDocumentRepository
{
    Task<KbDocument> CreateAsync(KbDocument document);
    Task<KbDocument?> GetByIdAsync(Guid id);
    Task<KbDocument?> GetByIdWithKnowledgeBaseAsync(Guid id);
    Task<List<KbDocument>> ListByKnowledgeBaseAsync(Guid knowledgeBaseId);
    Task SaveChangesAsync();
    void AddChunkRefs(IEnumerable<KbChunkRef> refs);
}
