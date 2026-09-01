using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

public interface IKnowledgeBaseRepository
{
    Task<KnowledgeBase> CreateAsync(KnowledgeBase kb);
    Task<KnowledgeBase?> GetByIdAsync(Guid id);
    Task<KnowledgeBase?> GetByNameAsync(string name);
    Task<List<KnowledgeBase>> ListAllAsync();
    Task<bool> DeleteAsync(Guid id);
}
