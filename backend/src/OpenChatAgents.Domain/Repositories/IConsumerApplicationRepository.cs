using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

public interface IConsumerApplicationRepository
{
    Task<ConsumerApplication> CreateAsync(string name, string clientId, string clientSecretHash, Guid? createdByUserId);
    Task<ConsumerApplication?> GetByIdAsync(Guid id);
    Task<ConsumerApplication?> GetByClientIdAsync(string clientId);
    Task<List<ConsumerApplication>> ListAllAsync();
    Task<bool> DeleteAsync(Guid id);
}
