using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Repositories;

public class MessageRepository(AppDbContext db)
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
