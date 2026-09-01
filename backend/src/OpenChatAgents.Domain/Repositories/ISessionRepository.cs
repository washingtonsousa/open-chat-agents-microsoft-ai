using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

public interface ISessionRepository
{
    Task<Session> CreateAsync(string title, Guid? agentId);
    Task<Session?> GetByIdAsync(Guid id);
    Task<List<Session>> ListAllAsync();
    Task<bool> DeleteAsync(Guid id);
}
