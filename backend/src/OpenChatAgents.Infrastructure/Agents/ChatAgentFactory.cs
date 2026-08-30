using Amazon;
using Amazon.BedrockRuntime;
using Amazon.Runtime;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenChatAgents.Infrastructure.Options;
using OpenChatAgents.Infrastructure.Telemetry;

namespace OpenChatAgents.Infrastructure.Agents;

public class ChatAgentFactory(IOptions<AppOptions> options)
{
    private readonly AppOptions _options = options.Value;

    public AIAgent Build(Models.Agent agent)
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
            },
        };

        var aiAgent = chatClient.AsAIAgent(agentOptions);

        if (!_options.Telemetry.Enabled)
            return aiAgent;

        return aiAgent.AsBuilder()
            .UseOpenTelemetry(TelemetryExtensions.AgentSourceName, otel => otel.EnableSensitiveData = _options.Telemetry.CaptureSensitiveContent)
            .Build();
    }

    public async IAsyncEnumerable<string> StreamAsync(Models.Agent agent, IEnumerable<ChatMessage> messages)
    {
        var aiAgent = Build(agent);
        await foreach (var update in aiAgent.RunStreamingAsync(messages))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
    }

    private IChatClient BuildChatClient(Models.Agent agent)
    {
        if (agent.Provider == Models.LlmProvider.Bedrock)
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
