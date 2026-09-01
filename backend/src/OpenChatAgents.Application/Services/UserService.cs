using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Exceptions;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Repositories;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

public class UserService(IUserRepository repo, IPasswordHasher hasher)
{
    public async Task<Models.User> CreateUserAsync(UserCreate payload, Guid createdByUserId)
    {
        var existing = await repo.GetByUsernameAsync(payload.Username);
        if (existing is not null)
            throw ApiException.Conflict($"Já existe um usuário com o nome '{payload.Username}'.");

        return await repo.CreateAsync(
            payload.Username,
            hasher.Hash(payload.Password),
            payload.IsAdmin,
            mustChangePassword: true,
            createdByUserId);
    }

    public Task<List<Models.User>> ListUsersAsync() => repo.ListAllAsync();

    public async Task DeleteUserAsync(Guid userId)
    {
        var deleted = await repo.DeleteAsync(userId);
        if (!deleted)
            throw ApiException.NotFound($"Usuário {userId} não encontrado.");
    }

    public async Task EnsureDefaultAdminAsync()
    {
        if (await repo.AnyAsync())
            return;

        await repo.CreateAsync("admin", hasher.Hash("1234"), isAdmin: true, mustChangePassword: true, createdByUserId: null);
    }
}
