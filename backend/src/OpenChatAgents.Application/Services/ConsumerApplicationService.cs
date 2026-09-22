using System.Security.Cryptography;
using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Exceptions;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Repositories;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

public class ConsumerApplicationService(IConsumerApplicationRepository repo, IPasswordHasher hasher)
{
    public async Task<ConsumerApplicationCreated> CreateAsync(ConsumerApplicationCreate payload, Guid createdByUserId)
    {
        var clientId = $"oca_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
        var clientSecret = $"ocs_{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()}";

        var app = await repo.CreateAsync(payload.Name.Trim(), clientId, hasher.Hash(clientSecret), createdByUserId);

        return new ConsumerApplicationCreated
        {
            Id = app.Id,
            Name = app.Name,
            ClientId = app.ClientId,
            ClientSecret = clientSecret,
            CreatedAt = app.CreatedAt,
        };
    }

    public Task<List<Models.ConsumerApplication>> ListAsync() => repo.ListAllAsync();

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await repo.DeleteAsync(id);
        if (!deleted)
            throw ApiException.NotFound($"Aplicação consumidora {id} não encontrada.");
    }
}
