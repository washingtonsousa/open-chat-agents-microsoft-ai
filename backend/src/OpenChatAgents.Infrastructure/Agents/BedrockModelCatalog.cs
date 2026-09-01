using Amazon;
using Amazon.Bedrock;
using Amazon.Bedrock.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Options;
using OpenChatAgents.Domain.Options;

namespace OpenChatAgents.Infrastructure.Agents;

public record ModelSummary(string Name, string? Provider);

public class BedrockModelCatalog(IOptions<AppOptions> options)
{
    private readonly AppOptions _options = options.Value;

    public async Task<List<ModelSummary>> ListModelsAsync()
    {
        try
        {
            var credentials = !string.IsNullOrEmpty(_options.Aws.AccessKeyId) && !string.IsNullOrEmpty(_options.Aws.SecretAccessKey)
                ? new BasicAWSCredentials(_options.Aws.AccessKeyId, _options.Aws.SecretAccessKey)
                : null;

            var region = RegionEndpoint.GetBySystemName(_options.Aws.Region);
            using var client = credentials is not null
                ? new AmazonBedrockClient(credentials, region)
                : new AmazonBedrockClient(region);

            var response = await client.ListFoundationModelsAsync(new ListFoundationModelsRequest
            {
                ByOutputModality = ModelModality.TEXT,
            });

            return [.. response.ModelSummaries
                .Where(m => m.ModelLifecycle?.Status == FoundationModelLifecycleStatus.ACTIVE)
                .Select(m => new ModelSummary(m.ModelId, m.ProviderName))];
        }
        catch
        {
            return [];
        }
    }
}
