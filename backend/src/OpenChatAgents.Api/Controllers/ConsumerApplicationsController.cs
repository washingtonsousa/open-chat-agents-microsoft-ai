using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Services;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/consumer-applications")]
[Authorize(Roles = "Admin")]
public class ConsumerApplicationsController(ConsumerApplicationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConsumerApplicationCreated>> Create([FromBody] ConsumerApplicationCreate payload)
    {
        var createdByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var created = await service.CreateAsync(payload, createdByUserId);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet]
    public async Task<ActionResult<ConsumerApplicationListResponse>> List()
    {
        var apps = await service.ListAsync();
        return Ok(new ConsumerApplicationListResponse
        {
            ConsumerApplications = [.. apps.Select(ConsumerApplicationResponse.FromEntity)],
            Total = apps.Count,
        });
    }

    [HttpDelete("{appId:guid}")]
    public async Task<IActionResult> Delete(Guid appId)
    {
        await service.DeleteAsync(appId);
        return NoContent();
    }
}
