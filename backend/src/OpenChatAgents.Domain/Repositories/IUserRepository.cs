using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Repositories;

public interface IUserRepository
{
    Task<User> CreateAsync(string username, string passwordHash, bool isAdmin, bool mustChangePassword, Guid? createdByUserId);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> AnyAsync();
    Task<List<User>> ListAllAsync();
    Task UpdatePasswordAsync(User user, string passwordHash, bool mustChangePassword);
    Task<bool> DeleteAsync(Guid id);
}
