using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Exceptions;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Repositories;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

public class McpServerService(IMcpServerRepository repo, ISecretProtector secretProtector)
{
    public async Task<Models.McpServer> CreateAsync(McpServerCreate payload, Guid createdByUserId)
    {
        var existing = await repo.GetByNameAsync(payload.Name.Trim());
        if (existing is not null)
            throw ApiException.Conflict($"Já existe um servidor MCP com o nome '{payload.Name}'.");

        var server = new Models.McpServer
        {
            Name = payload.Name.Trim(),
            Url = payload.Url.Trim(),
            AuthType = payload.AuthType,
            AuthHeaderName = payload.AuthType == Models.McpAuthType.Header ? payload.AuthHeaderName : null,
            Secret = string.IsNullOrEmpty(payload.Secret) ? null : secretProtector.Protect(payload.Secret),
            CreatedByUserId = createdByUserId,
        };

        return await repo.CreateAsync(server);
    }

    public async Task<Models.McpServer> GetAsync(Guid id)
    {
        var server = await repo.GetByIdAsync(id);
        if (server is null)
            throw ApiException.NotFound($"Servidor MCP {id} não encontrado.");
        return server;
    }

    public Task<List<Models.McpServer>> ListAsync() => repo.ListAllAsync();

    public async Task<Models.McpServer> UpdateAsync(Guid id, McpServerUpdate payload)
    {
        var server = await GetAsync(id);
        if (payload.Name is not null && payload.Name != server.Name)
        {
            var existing = await repo.GetByNameAsync(payload.Name);
            if (existing is not null)
                throw ApiException.Conflict($"Já existe um servidor MCP com o nome '{payload.Name}'.");
        }

        var update = new Domain.Repositories.McpServerUpdateFields(
            payload.Name,
            payload.Url,
            payload.AuthType,
            payload.AuthHeaderName,
            string.IsNullOrEmpty(payload.Secret) ? null : secretProtector.Protect(payload.Secret));

        return await repo.UpdateAsync(server, update);
    }

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await repo.DeleteAsync(id);
        if (!deleted)
            throw ApiException.NotFound($"Servidor MCP {id} não encontrado.");
    }
}
