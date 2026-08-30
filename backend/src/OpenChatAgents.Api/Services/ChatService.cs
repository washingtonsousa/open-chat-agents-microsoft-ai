using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenChatAgents.Infrastructure.Agents;
using OpenChatAgents.Api.Dtos;
using OpenChatAgents.Infrastructure.Options;
using OpenChatAgents.Infrastructure.Telemetry;
using OpenChatAgents.Api.Repositories;
using Models = OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Services;

public record SseEvent(string Event, object Data);

public class ChatService(
    MessageRepository messageRepo,
    SessionService sessionService,
    ChatAgentFactory chatAgentFactory,
    ModerationService moderation,
    KbRetrievalService kbRetrieval,
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

    public async IAsyncEnumerable<SseEvent> StreamMessageAsync(Guid sessionId, string userInput, Guid userId)
    {
        // Tags reconhecidas pelo Langfuse para agrupar traces nas telas de Sessions/Users.
        // Marcadas no span raiz (o da requisição HTTP, instrumentado pelo AddAspNetCoreInstrumentation)
        // para que todo o restante da trace herde a associação.
        Activity.Current?.SetTag("langfuse.session.id", sessionId.ToString());
        Activity.Current?.SetTag("langfuse.user.id", userId.ToString());

        using var activity = AppActivitySource.Source.StartActivity("chat.stream_message");
        activity?.SetTag("chat.session_id", sessionId);
        activity?.SetTag("langfuse.session.id", sessionId.ToString());
        activity?.SetTag("langfuse.user.id", userId.ToString());

        var session = await sessionService.GetSessionAsync(sessionId);
        activity?.SetTag("chat.agent_name", session.Agent?.Name ?? "default");

        var userMessage = await messageRepo.CreateAsync(sessionId, Models.MessageRole.User, userInput);
        yield return new SseEvent("user_message", MessageResponse.FromEntity(userMessage));

        string fullContent;
        var blocked = moderation.ContainsProfanity(userInput);
        activity?.SetTag("chat.moderation_blocked", blocked);

        if (blocked)
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

            var knowledgeBases = (session.Agent?.KnowledgeBaseLinks ?? [])
                .Select(l => l.KnowledgeBase)
                .Where(kb => kb is not null)
                .Select(kb => kb!)
                .ToList();

            string? retrievedContext;
            using (var retrievalActivity = AppActivitySource.Source.StartActivity("chat.kb_retrieval"))
            {
                retrievalActivity?.SetTag("chat.kb_count", knowledgeBases.Count);
                retrievalActivity?.SetTag("langfuse.session.id", sessionId.ToString());
                retrievalActivity?.SetTag("langfuse.user.id", userId.ToString());
                retrievedContext = await kbRetrieval.BuildContextAsync(knowledgeBases, userInput);
                retrievalActivity?.SetTag("chat.kb_context_found", retrievedContext is not null);
            }

            if (retrievedContext is not null)
                messages.Insert(0, new ChatMessage(ChatRole.System, retrievedContext));

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
