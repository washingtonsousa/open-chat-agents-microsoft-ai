using OpenChatAgents.Api.Repositories;

namespace OpenChatAgents.Api.Services;

public class SessionService(SessionRepository repo)
{
    public Task<Models.Session> CreateSessionAsync(string title = "Nova conversa", Guid? agentId = null) =>
        repo.CreateAsync(title, agentId);

    public async Task<Models.Session> GetSessionAsync(Guid sessionId)
    {
        var session = await repo.GetByIdAsync(sessionId);
        if (session is null)
            throw ApiException.NotFound($"Sessão {sessionId} não encontrada.");
        return session;
    }

    public Task<List<Models.Session>> ListSessionsAsync() => repo.ListAllAsync();

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var deleted = await repo.DeleteAsync(sessionId);
        if (!deleted)
            throw ApiException.NotFound($"Sessão {sessionId} não encontrada.");
    }
}
