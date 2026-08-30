using Amazon;
using Amazon.BedrockRuntime;
using Amazon.Runtime;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenChatAgents.Infrastructure.Options;
using OpenChatAgents.Infrastructure.Telemetry;

namespace OpenChatAgents.Infrastructure.Agents;

public class EmbeddingClientFactory(IOptions<AppOptions> options)
{
    private readonly AppOptions _options = options.Value;

    public IEmbeddingGenerator<string, Embedding<float>> Build(string provider, string model)
    {
        var generator = BuildGenerator(provider, model);

        if (!_options.Telemetry.Enabled)
            return generator;

        return generator.AsBuilder()
            .UseOpenTelemetry(sourceName: TelemetryExtensions.AgentSourceName, configure: otel => otel.EnableSensitiveData = _options.Telemetry.CaptureSensitiveContent)
            .Build();
    }

    private IEmbeddingGenerator<string, Embedding<float>> BuildGenerator(string provider, string model)
    {
        if (provider == Models.LlmProvider.Bedrock)
        {
            var credentials = !string.IsNullOrEmpty(_options.Aws.AccessKeyId) && !string.IsNullOrEmpty(_options.Aws.SecretAccessKey)
                ? new BasicAWSCredentials(_options.Aws.AccessKeyId, _options.Aws.SecretAccessKey)
                : null;

            var region = RegionEndpoint.GetBySystemName(_options.Aws.Region);
            var runtime = credentials is not null
                ? new AmazonBedrockRuntimeClient(credentials, region)
                : new AmazonBedrockRuntimeClient(region);

            return runtime.AsIEmbeddingGenerator(model);
        }

        return new OllamaApiClient(new Uri(_options.OllamaBaseUrl), model);
    }

    public async Task<int> DetectDimensionsAsync(string provider, string model)
    {
        var generator = Build(provider, model);
        var result = await generator.GenerateAsync(["dimension probe"]);
        return result[0].Vector.Length;
    }
}
