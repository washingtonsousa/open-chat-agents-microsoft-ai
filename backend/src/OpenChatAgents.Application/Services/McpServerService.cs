using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Exceptions;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Repositories;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

public class McpServerService(IMcpServerRepository repo, ISecretProtector secretProtector)
{
    /// <summary>Built-in tool groups always available on the platform (Name, BuiltInKey, Description).</summary>
    private static readonly (string Name, string Key, string Description)[] BuiltInServers =
    [
        ("Sistema de arquivos", "filesystem", "Ler e escrever arquivos numa pasta permitida do servidor."),
        ("Data e hora", "datetime", "Consultar data/hora atual e converter entre fusos horários."),
        ("Criador de skills", "skill-creator", "Criar uma nova skill (instrução reutilizável) a partir do pedido do usuário no chat."),
    ];

    /// <summary>Idempotent — upserts the built-in McpServer rows by BuiltInKey. Safe to call on every startup.</summary>
    public async Task EnsureBuiltInServersAsync()
    {
        var existing = await repo.ListAllAsync();
        foreach (var (name, key, description) in BuiltInServers)
        {
            if (existing.Any(s => s.BuiltInKey == key))
                continue;

            await repo.CreateAsync(new Models.McpServer
            {
                Name = name,
                Description = description,
                Kind = Models.McpServerKind.BuiltIn,
                BuiltInKey = key,
                AuthType = Models.McpAuthType.None,
            });
        }
    }

    public async Task<Models.McpServer> CreateAsync(McpServerCreate payload, Guid createdByUserId)
    {
        var existing = await repo.GetByNameAsync(payload.Name.Trim());
        if (existing is not null)
            throw ApiException.Conflict($"Já existe um servidor MCP com o nome '{payload.Name}'.");

        var server = new Models.McpServer
        {
            Name = payload.Name.Trim(),
            Description = payload.Description,
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
        if (server.Kind == Models.McpServerKind.BuiltIn)
            throw ApiException.Conflict("Servidores MCP embutidos não podem ser editados.");

        if (payload.Name is not null && payload.Name != server.Name)
        {
            var existing = await repo.GetByNameAsync(payload.Name);
            if (existing is not null)
                throw ApiException.Conflict($"Já existe um servidor MCP com o nome '{payload.Name}'.");
        }

        var update = new Domain.Repositories.McpServerUpdateFields(
            payload.Name,
            payload.Description,
            payload.Url,
            payload.AuthType,
            payload.AuthHeaderName,
            string.IsNullOrEmpty(payload.Secret) ? null : secretProtector.Protect(payload.Secret));

        return await repo.UpdateAsync(server, update);
    }

    public async Task DeleteAsync(Guid id)
    {
        var server = await GetAsync(id);
        if (server.Kind == Models.McpServerKind.BuiltIn)
            throw ApiException.Conflict("Servidores MCP embutidos não podem ser excluídos.");

        await repo.DeleteAsync(id);
    }
}
