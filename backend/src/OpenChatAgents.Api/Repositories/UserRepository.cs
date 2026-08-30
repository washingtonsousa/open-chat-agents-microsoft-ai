using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Infrastructure.Data;
using OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Repositories;

public class UserRepository(AppDbContext db)
{
    public async Task<User> CreateAsync(string username, string passwordHash, bool isAdmin, bool mustChangePassword, Guid? createdByUserId)
    {
        var user = new User
        {
            Username = username,
            PasswordHash = passwordHash,
            IsAdmin = isAdmin,
            MustChangePassword = mustChangePassword,
            CreatedByUserId = createdByUserId,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public Task<User?> GetByIdAsync(Guid id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public Task<bool> AnyAsync() => db.Users.AnyAsync();

    public Task<List<User>> ListAllAsync() =>
        db.Users.OrderBy(u => u.Username).ToListAsync();

    public async Task UpdatePasswordAsync(User user, string passwordHash, bool mustChangePassword)
    {
        user.PasswordHash = passwordHash;
        user.MustChangePassword = mustChangePassword;
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return true;
    }
}
