using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Domain.Models;
using OpenChatAgents.Domain.Repositories;
using OpenChatAgents.Infrastructure.Data;

namespace OpenChatAgents.Infrastructure.Persistence;

public class ConsumerApplicationRepository(AppDbContext db) : IConsumerApplicationRepository
{
    public async Task<ConsumerApplication> CreateAsync(string name, string clientId, string clientSecretHash, Guid? createdByUserId)
    {
        var app = new ConsumerApplication
        {
            Name = name,
            ClientId = clientId,
            ClientSecretHash = clientSecretHash,
            CreatedByUserId = createdByUserId,
        };
        db.ConsumerApplications.Add(app);
        await db.SaveChangesAsync();
        return app;
    }

    public Task<ConsumerApplication?> GetByIdAsync(Guid id) =>
        db.ConsumerApplications.FirstOrDefaultAsync(a => a.Id == id);

    public Task<ConsumerApplication?> GetByClientIdAsync(string clientId) =>
        db.ConsumerApplications.FirstOrDefaultAsync(a => a.ClientId == clientId);

    public Task<List<ConsumerApplication>> ListAllAsync() =>
        db.ConsumerApplications
            .Include(a => a.CreatedByUser)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<bool> DeleteAsync(Guid id)
    {
        var app = await GetByIdAsync(id);
        if (app is null) return false;
        db.ConsumerApplications.Remove(app);
        await db.SaveChangesAsync();
        return true;
    }
}
