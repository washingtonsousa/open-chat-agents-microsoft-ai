using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Services;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/mcp-servers")]
public class McpServersController(McpServerService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<McpServerResponse>> Create([FromBody] McpServerCreate payload)
    {
        var server = await service.CreateAsync(payload, CurrentUserId());
        return StatusCode(StatusCodes.Status201Created, McpServerResponse.FromEntity(server));
    }

    [HttpGet]
    public async Task<ActionResult<McpServerListResponse>> List()
    {
        var servers = await service.ListAsync();
        return Ok(new McpServerListResponse
        {
            McpServers = [.. servers.Select(McpServerResponse.FromEntity)],
            Total = servers.Count,
        });
    }

    [HttpGet("{serverId:guid}")]
    public async Task<ActionResult<McpServerResponse>> Get(Guid serverId)
    {
        var server = await service.GetAsync(serverId);
        return Ok(McpServerResponse.FromEntity(server));
    }

    [HttpPut("{serverId:guid}")]
    public async Task<ActionResult<McpServerResponse>> Update(Guid serverId, [FromBody] McpServerUpdate payload)
    {
        var server = await service.UpdateAsync(serverId, payload);
        return Ok(McpServerResponse.FromEntity(server));
    }

    [HttpDelete("{serverId:guid}")]
    public async Task<IActionResult> Delete(Guid serverId)
    {
        await service.DeleteAsync(serverId);
        return NoContent();
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
