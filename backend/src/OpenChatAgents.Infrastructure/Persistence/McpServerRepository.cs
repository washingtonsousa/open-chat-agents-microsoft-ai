using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Domain.Models;
using OpenChatAgents.Domain.Repositories;
using OpenChatAgents.Infrastructure.Data;

namespace OpenChatAgents.Infrastructure.Persistence;

public class McpServerRepository(AppDbContext db) : IMcpServerRepository
{
    public async Task<McpServer> CreateAsync(McpServer server)
    {
        db.McpServers.Add(server);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(server.Id))!;
    }

    public Task<McpServer?> GetByIdAsync(Guid id) =>
        db.McpServers.Include(s => s.CreatedByUser).FirstOrDefaultAsync(s => s.Id == id);

    public Task<McpServer?> GetByNameAsync(string name) =>
        db.McpServers.FirstOrDefaultAsync(s => s.Name == name);

    public Task<List<McpServer>> ListAllAsync() =>
        db.McpServers
            .Include(s => s.CreatedByUser)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public async Task<McpServer> UpdateAsync(McpServer server, McpServerUpdateFields update)
    {
        if (update.Name is not null) server.Name = update.Name;
        if (update.Url is not null) server.Url = update.Url;
        if (update.AuthType is not null) server.AuthType = update.AuthType;
        if (update.AuthHeaderName is not null) server.AuthHeaderName = update.AuthHeaderName;
        if (update.Secret is not null) server.Secret = update.Secret;
        server.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return (await GetByIdAsync(server.Id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var server = await db.McpServers.FirstOrDefaultAsync(s => s.Id == id);
        if (server is null) return false;
        db.McpServers.Remove(server);
        await db.SaveChangesAsync();
        return true;
    }
}
