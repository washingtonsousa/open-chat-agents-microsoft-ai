using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

public interface IMessageRepository
{
    Task<Message> CreateAsync(Guid sessionId, string role, string content, string? imageObjectKey = null, string? imageContentType = null);
    Task<Message?> GetByIdAsync(Guid id);
    Task<List<Message>> ListBySessionAsync(Guid sessionId);
}
