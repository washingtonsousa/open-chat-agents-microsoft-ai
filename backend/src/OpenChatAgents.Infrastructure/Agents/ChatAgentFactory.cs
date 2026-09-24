using System.Text;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.Runtime;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Models;
using OpenChatAgents.Domain.Options;
using OpenChatAgents.Infrastructure.Telemetry;

namespace OpenChatAgents.Infrastructure.Agents;

public class ChatAgentFactory(IOptions<AppOptions> options, IMcpToolFactory mcpToolFactory) : IChatAgentFactory
{
    private readonly AppOptions _options = options.Value;

    public AIAgent Build(Agent agent, IReadOnlyList<AITool>? tools = null)
    {
        var chatClient = BuildChatClient(agent);

        var agentOptions = new ChatClientAgentOptions
        {
            Name = agent.Name,
            ChatOptions = new ChatOptions
            {
                Instructions = BuildInstructions(agent),
                Temperature = (float)agent.Temperature,
                MaxOutputTokens = agent.MaxTokens,
                Tools = tools is { Count: > 0 } ? [.. tools] : null,
            },
        };

        var aiAgent = chatClient.AsAIAgent(agentOptions);

        if (!_options.Telemetry.Enabled)
            return aiAgent;

        return aiAgent.AsBuilder()
            .UseOpenTelemetry(TelemetryExtensions.AgentSourceName, otel => otel.EnableSensitiveData = _options.Telemetry.CaptureSensitiveContent)
            .Build();
    }

    public async IAsyncEnumerable<string> StreamAsync(Agent agent, IEnumerable<ChatMessage> messages, Guid userId)
    {
        var context = new BuiltInToolContext(userId);

        var mcpServers = agent.McpServerLinks
            .Where(l => l.McpServer is not null)
            .Select(l => l.McpServer!)
            .ToList();

        // A conexão MCP precisa ficar viva durante toda a invocação de tools, não só a listagem —
        // por isso a sessão é criada por turno de chat e só descartada depois do streaming terminar.
        await using var mcpSession = await mcpToolFactory.CreateSessionAsync(mcpServers, context);

        var tools = new List<AITool>(mcpSession.Tools);
        var subAgentSessions = new List<IMcpToolSession>();

        try
        {
            // Sub-agentes viram ferramentas (orquestrador). De propósito, só um nível: as próprias
            // ligações de sub-agente do FILHO nunca são expandidas — é isso que impede recursão infinita.
            foreach (var link in agent.SubAgentLinks)
            {
                if (link.SubAgent is null) continue;
                var subAgent = link.SubAgent;

                var subServers = subAgent.McpServerLinks
                    .Where(l => l.McpServer is not null)
                    .Select(l => l.McpServer!)
                    .ToList();
                var subSession = await mcpToolFactory.CreateSessionAsync(subServers, context);
                subAgentSessions.Add(subSession);

                var subAiAgent = Build(subAgent, subSession.Tools);
                tools.Add(subAiAgent.AsAIFunction(new AIFunctionFactoryOptions
                {
                    Name = Slugify(subAgent.Name),
                    Description = $"Consulta o agente '{subAgent.Name}'. {subAgent.SystemPrompt}",
                }));
            }

            var aiAgent = Build(agent, tools);
            await foreach (var update in aiAgent.RunStreamingAsync(messages))
            {
                if (!string.IsNullOrEmpty(update.Text))
                    yield return update.Text;
            }
        }
        finally
        {
            foreach (var session in subAgentSessions)
                await session.DisposeAsync();
        }
    }

    private static string BuildInstructions(Agent agent)
    {
        var skills = agent.SkillLinks
            .Where(l => l.Skill is not null)
            .Select(l => l.Skill!)
            .ToList();

        if (skills.Count == 0)
            return agent.SystemPrompt;

        var sb = new StringBuilder(agent.SystemPrompt);
        foreach (var skill in skills)
        {
            sb.Append("\n\n---\n\n## Skill: ").Append(skill.Name).Append('\n').Append(skill.Content);
        }
        return sb.ToString();
    }

    private static string Slugify(string name)
    {
        var slug = new string([.. name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_')]);
        return $"ask_{slug}";
    }

    private IChatClient BuildChatClient(Agent agent)
    {
        if (agent.Provider == LlmProvider.Bedrock)
        {
            var credentials = !string.IsNullOrEmpty(_options.Aws.AccessKeyId) && !string.IsNullOrEmpty(_options.Aws.SecretAccessKey)
                ? new BasicAWSCredentials(_options.Aws.AccessKeyId, _options.Aws.SecretAccessKey)
                : null;

            var region = RegionEndpoint.GetBySystemName(_options.Aws.Region);
            var runtime = credentials is not null
                ? new AmazonBedrockRuntimeClient(credentials, region)
                : new AmazonBedrockRuntimeClient(region);

            return runtime.AsIChatClient(agent.LlmModel);
        }

        return new OllamaApiClient(new Uri(_options.OllamaBaseUrl), agent.LlmModel);
    }
}
