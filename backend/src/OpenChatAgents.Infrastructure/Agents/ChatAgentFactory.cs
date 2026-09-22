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
                Instructions = agent.SystemPrompt,
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

    public async IAsyncEnumerable<string> StreamAsync(Agent agent, IEnumerable<ChatMessage> messages)
    {
        var mcpServers = agent.McpServerLinks
            .Where(l => l.McpServer is not null)
            .Select(l => l.McpServer!)
            .ToList();

        // A conexão MCP precisa ficar viva durante toda a invocação de tools, não só a listagem —
        // por isso a sessão é criada por turno de chat e só descartada depois do streaming terminar.
        await using var mcpSession = await mcpToolFactory.CreateSessionAsync(mcpServers);

        var aiAgent = Build(agent, mcpSession.Tools);
        await foreach (var update in aiAgent.RunStreamingAsync(messages))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
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
