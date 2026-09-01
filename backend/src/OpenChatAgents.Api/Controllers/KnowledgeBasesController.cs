using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Services;
using OpenChatAgents.Application.Exceptions;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/knowledge-bases")]
public class KnowledgeBasesController(KnowledgeBaseService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<KnowledgeBaseResponse>> Create([FromBody] KnowledgeBaseCreate payload)
    {
        var kb = await service.CreateKbAsync(payload, CurrentUserId());
        return StatusCode(StatusCodes.Status201Created, KnowledgeBaseResponse.FromEntity(kb));
    }

    [HttpGet]
    public async Task<ActionResult<KnowledgeBaseListResponse>> List()
    {
        var kbs = await service.ListKbsAsync();
        return Ok(new KnowledgeBaseListResponse
        {
            KnowledgeBases = [.. kbs.Select(KnowledgeBaseResponse.FromEntity)],
            Total = kbs.Count,
        });
    }

    [HttpGet("{kbId:guid}")]
    public async Task<ActionResult<KnowledgeBaseResponse>> Get(Guid kbId)
    {
        var kb = await service.GetKbAsync(kbId);
        return Ok(KnowledgeBaseResponse.FromEntity(kb));
    }

    [HttpDelete("{kbId:guid}")]
    public async Task<IActionResult> Delete(Guid kbId)
    {
        await service.DeleteKbAsync(kbId);
        return NoContent();
    }

    [HttpPost("{kbId:guid}/documents")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<KbDocumentResponse>> UploadDocument(Guid kbId, IFormFile file)
    {
        if (file.Length == 0)
            throw ApiException.Conflict("Arquivo vazio.");

        await using var stream = file.OpenReadStream();
        var document = await service.UploadDocumentAsync(kbId, file.FileName, file.ContentType, stream, file.Length);
        return StatusCode(StatusCodes.Status201Created, KbDocumentResponse.FromEntity(document));
    }

    [HttpGet("{kbId:guid}/documents")]
    public async Task<ActionResult<KbDocumentListResponse>> ListDocuments(Guid kbId)
    {
        var documents = await service.ListDocumentsAsync(kbId);
        return Ok(new KbDocumentListResponse { Documents = [.. documents.Select(KbDocumentResponse.FromEntity)] });
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
