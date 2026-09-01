using Microsoft.Extensions.AI;

namespace OpenChatAgents.Domain.Abstractions;

public interface IEmbeddingClientFactory
{
    IEmbeddingGenerator<string, Embedding<float>> Build(string provider, string model);
    Task<int> DetectDimensionsAsync(string provider, string model);
}
