using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenChatAgents.Api.Agents;
using OpenChatAgents.Api.Dtos;
using OpenChatAgents.Api.Options;
using OpenChatAgents.Api.Repositories;

namespace OpenChatAgents.Api.Services;

public record SseEvent(string Event, object Data);

public class ChatService(
    MessageRepository messageRepo,
    SessionService sessionService,
    ChatAgentFactory chatAgentFactory,
    ModerationService moderation,
    IOptions<AppOptions> appOptions)
{
    private const string DefaultSystemPrompt = """
        Você é um assistente prestativo e amigável.
        Regras que você deve seguir sem exceção:
        - Nunca use linguagem ofensiva, palavrões ou termos inadequados.
        - Não responda perguntas que contenham linguagem inapropriada.
        - Seja sempre respeitoso, claro e objetivo nas respostas.
        - Se não souber a resposta, diga que não sabe ao invés de inventar.
        """;

    public async IAsyncEnumerable<SseEvent> StreamMessageAsync(Guid sessionId, string userInput)
    {
        var session = await sessionService.GetSessionAsync(sessionId);

        var userMessage = await messageRepo.CreateAsync(sessionId, Models.MessageRole.User, userInput);
        yield return new SseEvent("user_message", MessageResponse.FromEntity(userMessage));

        string fullContent;
        if (moderation.ContainsProfanity(userInput))
        {
            fullContent = moderation.GetViolationResponse();
            yield return new SseEvent("chunk", new { content = fullContent });
        }
        else
        {
            var effectiveAgent = session.Agent ?? new Models.Agent
            {
                Name = "default",
                Provider = Models.LlmProvider.Ollama,
                LlmModel = appOptions.Value.LlmModel,
                Temperature = 0.7,
                SystemPrompt = DefaultSystemPrompt,
            };

            var messages = await BuildMessagesAsync(sessionId);
            var sb = new StringBuilder();
            await foreach (var token in chatAgentFactory.StreamAsync(effectiveAgent, messages))
            {
                sb.Append(token);
                yield return new SseEvent("chunk", new { content = token });
            }
            fullContent = sb.ToString();
        }

        var assistantMessage = await messageRepo.CreateAsync(sessionId, Models.MessageRole.Assistant, fullContent);
        yield return new SseEvent("done", MessageResponse.FromEntity(assistantMessage));
    }

    public async Task<List<Models.Message>> GetHistoryAsync(Guid sessionId)
    {
        await sessionService.GetSessionAsync(sessionId);
        return await messageRepo.ListBySessionAsync(sessionId);
    }

    private async Task<List<ChatMessage>> BuildMessagesAsync(Guid sessionId)
    {
        var history = await messageRepo.ListBySessionAsync(sessionId);
        return [.. history.Select(m => new ChatMessage(
            m.Role == Models.MessageRole.User ? ChatRole.User : ChatRole.Assistant,
            m.Content))];
    }
}
