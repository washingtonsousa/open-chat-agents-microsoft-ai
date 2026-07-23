using Microsoft.AspNetCore.Mvc;
using OpenChatAgents.Api.Dtos;
using OpenChatAgents.Api.Services;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/agents")]
public class AgentsController(AgentService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AgentResponse>> Create([FromBody] AgentCreate payload)
    {
        var agent = await service.CreateAgentAsync(payload);
        return StatusCode(StatusCodes.Status201Created, AgentResponse.FromEntity(agent));
    }

    [HttpGet]
    public async Task<ActionResult<AgentListResponse>> List()
    {
        var agents = await service.ListAgentsAsync();
        return Ok(new AgentListResponse
        {
            Agents = [.. agents.Select(AgentResponse.FromEntity)],
            Total = agents.Count,
        });
    }

    [HttpGet("{agentId:guid}")]
    public async Task<ActionResult<AgentResponse>> Get(Guid agentId)
    {
        var agent = await service.GetAgentAsync(agentId);
        return Ok(AgentResponse.FromEntity(agent));
    }

    [HttpPut("{agentId:guid}")]
    public async Task<ActionResult<AgentResponse>> Update(Guid agentId, [FromBody] AgentUpdate payload)
    {
        var agent = await service.UpdateAgentAsync(agentId, payload);
        return Ok(AgentResponse.FromEntity(agent));
    }

    [HttpDelete("{agentId:guid}")]
    public async Task<IActionResult> Delete(Guid agentId)
    {
        await service.DeleteAgentAsync(agentId);
        return NoContent();
    }
}
