using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Exceptions;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Repositories;
using OpenChatAgents.Domain.VectorStore;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

public class KnowledgeBaseService(
    IKnowledgeBaseRepository kbRepo,
    IKbDocumentRepository documentRepo,
    IEmbeddingClientFactory embeddingClientFactory,
    IObjectStore objectStore,
    IKbVectorStore vectorStore)
{
    public async Task<Models.KnowledgeBase> CreateKbAsync(KnowledgeBaseCreate payload, Guid createdByUserId)
    {
        var existing = await kbRepo.GetByNameAsync(payload.Name.Trim());
        if (existing is not null)
            throw ApiException.Conflict($"Já existe uma base de conhecimento com o nome '{payload.Name}'.");

        int dimensions;
        try
        {
            dimensions = await embeddingClientFactory.DetectDimensionsAsync(payload.EmbeddingProvider, payload.EmbeddingModel);
        }
        catch (Exception ex)
        {
            throw ApiException.BadGateway($"Não foi possível gerar um embedding de teste com o modelo '{payload.EmbeddingModel}': {ex.Message}");
        }

        var kb = new Models.KnowledgeBase
        {
            Name = payload.Name.Trim(),
            Description = payload.Description,
            ChunkSize = payload.ChunkSize,
            ChunkOverlap = payload.ChunkOverlap,
            EmbeddingProvider = payload.EmbeddingProvider,
            EmbeddingModel = payload.EmbeddingModel,
            EmbeddingDimensions = dimensions,
            CreatedByUserId = createdByUserId,
        };

        return await kbRepo.CreateAsync(kb);
    }

    public async Task<Models.KnowledgeBase> GetKbAsync(Guid id)
    {
        var kb = await kbRepo.GetByIdAsync(id);
        if (kb is null)
            throw ApiException.NotFound($"Base de conhecimento {id} não encontrada.");
        return kb;
    }

    public Task<List<Models.KnowledgeBase>> ListKbsAsync() => kbRepo.ListAllAsync();

    public async Task DeleteKbAsync(Guid id)
    {
        var kb = await GetKbAsync(id);
        await vectorStore.DeleteCollectionAsync(kb);
        await objectStore.RemovePrefixAsync($"kb/{kb.Id}/");
        await kbRepo.DeleteAsync(id);
    }

    public async Task<Models.KbDocument> UploadDocumentAsync(Guid kbId, string fileName, string contentType, Stream content, long size)
    {
        var kb = await GetKbAsync(kbId);

        var documentId = Guid.NewGuid();
        var document = new Models.KbDocument
        {
            Id = documentId,
            KnowledgeBaseId = kb.Id,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = size,
            ObjectKey = $"kb/{kb.Id}/{documentId}/{fileName}",
        };

        await documentRepo.CreateAsync(document);
        await objectStore.PutObjectAsync(document.ObjectKey, content, size, contentType);

        return document;
    }

    public async Task<List<Models.KbDocument>> ListDocumentsAsync(Guid kbId)
    {
        await GetKbAsync(kbId);
        return await documentRepo.ListByKnowledgeBaseAsync(kbId);
    }
}
