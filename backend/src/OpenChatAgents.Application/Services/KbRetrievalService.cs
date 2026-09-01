using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Telemetry;
using OpenChatAgents.Domain.VectorStore;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

public class KbRetrievalService(IEmbeddingClientFactory embeddingClientFactory, IKbVectorStore vectorStore)
{
    public async Task<string?> BuildContextAsync(IEnumerable<Models.KnowledgeBase> knowledgeBases, string query, int topPerKb = 5)
    {
        var kbs = knowledgeBases.ToList();
        if (kbs.Count == 0)
            return null;

        var blocks = new List<string>();
        foreach (var kb in kbs)
        {
            using var activity = AppActivitySource.Source.StartActivity("kb.search");
            activity?.SetTag("kb.id", kb.Id);
            activity?.SetTag("kb.name", kb.Name);
            activity?.SetTag("kb.embedding_model", kb.EmbeddingModel);

            var embeddingGenerator = embeddingClientFactory.Build(kb.EmbeddingProvider, kb.EmbeddingModel);
            var queryEmbedding = (await embeddingGenerator.GenerateAsync([query]))[0].Vector;
            var results = await vectorStore.SearchAsync(kb, queryEmbedding, topPerKb);
            activity?.SetTag("kb.results_count", results.Count);

            blocks.AddRange(results.Select(r => $"[{kb.Name}] {r.Content}"));
        }

        if (blocks.Count == 0)
            return null;

        return "Use o contexto abaixo, recuperado das bases de conhecimento associadas, para responder à pergunta do usuário quando for relevante. " +
               "Se o contexto não ajudar, responda normalmente.\n\n" + string.Join("\n---\n", blocks);
    }
}
