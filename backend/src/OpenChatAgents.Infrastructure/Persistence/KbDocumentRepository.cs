using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Domain.Models;
using OpenChatAgents.Domain.Repositories;
using OpenChatAgents.Infrastructure.Data;

namespace OpenChatAgents.Infrastructure.Persistence;

public class KbDocumentRepository(AppDbContext db) : IKbDocumentRepository
{
    public async Task<KbDocument> CreateAsync(KbDocument document)
    {
        db.KbDocuments.Add(document);
        await db.SaveChangesAsync();
        return document;
    }

    public Task<KbDocument?> GetByIdAsync(Guid id) =>
        db.KbDocuments.FirstOrDefaultAsync(d => d.Id == id);

    public Task<KbDocument?> GetByIdWithKnowledgeBaseAsync(Guid id) =>
        db.KbDocuments.Include(d => d.KnowledgeBase).FirstOrDefaultAsync(d => d.Id == id);

    public Task<List<KbDocument>> ListByKnowledgeBaseAsync(Guid knowledgeBaseId) =>
        db.KbDocuments
            .Where(d => d.KnowledgeBaseId == knowledgeBaseId)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    public void AddChunkRefs(IEnumerable<KbChunkRef> refs) => db.KbChunkRefs.AddRange(refs);
}
