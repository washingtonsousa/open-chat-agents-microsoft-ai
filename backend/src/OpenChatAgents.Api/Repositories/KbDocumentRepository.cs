using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Repositories;

public class KbDocumentRepository(AppDbContext db)
{
    public async Task<KbDocument> CreateAsync(KbDocument document)
    {
        db.KbDocuments.Add(document);
        await db.SaveChangesAsync();
        return document;
    }

    public Task<KbDocument?> GetByIdAsync(Guid id) =>
        db.KbDocuments.FirstOrDefaultAsync(d => d.Id == id);

    public Task<List<KbDocument>> ListByKnowledgeBaseAsync(Guid knowledgeBaseId) =>
        db.KbDocuments
            .Where(d => d.KnowledgeBaseId == knowledgeBaseId)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();
}
