using System.ComponentModel.DataAnnotations;
using Models = OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Dtos;

public class KnowledgeBaseCreate
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(100, 20_000)]
    public int ChunkSize { get; set; } = 1000;

    [Range(0, 5_000)]
    public int ChunkOverlap { get; set; } = 200;

    [RegularExpression("^(ollama|bedrock)$")]
    public string EmbeddingProvider { get; set; } = "ollama";

    [Required, StringLength(100, MinimumLength = 1)]
    public string EmbeddingModel { get; set; } = string.Empty;
}

public class KnowledgeBaseResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ChunkSize { get; set; }
    public int ChunkOverlap { get; set; }
    public string EmbeddingProvider { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public int EmbeddingDimensions { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public UserResponse? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static KnowledgeBaseResponse FromEntity(Models.KnowledgeBase kb) => new()
    {
        Id = kb.Id,
        Name = kb.Name,
        Description = kb.Description,
        ChunkSize = kb.ChunkSize,
        ChunkOverlap = kb.ChunkOverlap,
        EmbeddingProvider = kb.EmbeddingProvider,
        EmbeddingModel = kb.EmbeddingModel,
        EmbeddingDimensions = kb.EmbeddingDimensions,
        Status = ComputeStatus(kb),
        DocumentCount = kb.Documents.Count,
        CreatedBy = kb.CreatedByUser is null ? null : UserResponse.FromEntity(kb.CreatedByUser),
        CreatedAt = kb.CreatedAt,
        UpdatedAt = kb.UpdatedAt,
    };

    private static string ComputeStatus(Models.KnowledgeBase kb)
    {
        if (kb.Documents.Count == 0) return "empty";
        if (kb.Documents.Any(d => d.Status is Models.KbDocumentStatus.Uploaded or Models.KbDocumentStatus.Processing)) return "processing";
        if (kb.Documents.Any(d => d.Status == Models.KbDocumentStatus.Failed)) return "failed";
        return "ready";
    }
}

public class KnowledgeBaseListResponse
{
    public List<KnowledgeBaseResponse> KnowledgeBases { get; set; } = [];
    public int Total { get; set; }
}

public class KbDocumentResponse
{
    public Guid Id { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int ChunkCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }

    public static KbDocumentResponse FromEntity(Models.KbDocument doc) => new()
    {
        Id = doc.Id,
        KnowledgeBaseId = doc.KnowledgeBaseId,
        FileName = doc.FileName,
        ContentType = doc.ContentType,
        SizeBytes = doc.SizeBytes,
        Status = doc.Status,
        ErrorMessage = doc.ErrorMessage,
        ChunkCount = doc.ChunkCount,
        CreatedAt = doc.CreatedAt,
        ProcessedAt = doc.ProcessedAt,
    };
}

public class KbDocumentListResponse
{
    public List<KbDocumentResponse> Documents { get; set; } = [];
}
