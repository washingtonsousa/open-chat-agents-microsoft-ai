using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Domain.Models;
using OpenChatAgents.Domain.Repositories;
using OpenChatAgents.Infrastructure.Data;

namespace OpenChatAgents.Infrastructure.Persistence;

public class MessageRepository(AppDbContext db) : IMessageRepository
{
    public async Task<Message> CreateAsync(Guid sessionId, string role, string content)
    {
        var message = new Message { SessionId = sessionId, Role = role, Content = content };
        db.Messages.Add(message);
        await db.SaveChangesAsync();
        return message;
    }

    public Task<List<Message>> ListBySessionAsync(Guid sessionId) =>
        db.Messages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
}
