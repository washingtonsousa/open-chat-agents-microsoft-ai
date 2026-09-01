using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

public interface IMessageRepository
{
    Task<Message> CreateAsync(Guid sessionId, string role, string content);
    Task<List<Message>> ListBySessionAsync(Guid sessionId);
}
