using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenChatAgents.Infrastructure.Agents;
using OpenChatAgents.Api.Dtos;
using OpenChatAgents.Infrastructure.Options;
using OpenChatAgents.Api.Services;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/models")]
public class ModelsController(IHttpClientFactory httpClientFactory, IOptions<AppOptions> appOptions, BedrockModelCatalog bedrockCatalog) : ControllerBase
{
    [HttpGet("ollama")]
    public async Task<ActionResult<ModelsResponse>> ListOllama()
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        OllamaTagsResponse? data;
        try
        {
            using var response = await client.GetAsync($"{appOptions.Value.OllamaBaseUrl}/api/tags");
            response.EnsureSuccessStatusCode();
            data = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>();
        }
        catch (HttpRequestException)
        {
            throw ApiException.BadGateway("Não foi possível conectar ao Ollama. Verifique se o serviço está rodando.");
        }

        return Ok(new ModelsResponse
        {
            Models = [.. (data?.Models ?? []).Select(m => new ModelInfo
            {
                Name = m.Name,
                Provider = "ollama",
                Size = m.Size,
            })],
        });
    }

    [HttpGet("bedrock")]
    public async Task<ActionResult<ModelsResponse>> ListBedrock()
    {
        var models = await bedrockCatalog.ListModelsAsync();
        return Ok(new ModelsResponse
        {
            Models = [.. models.Select(m => new ModelInfo { Name = m.Name, Provider = m.Provider })],
        });
    }

    private class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaTagModel> Models { get; set; } = [];
    }

    private class OllamaTagModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long? Size { get; set; }
    }
}
