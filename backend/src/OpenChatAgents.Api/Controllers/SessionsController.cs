using Microsoft.AspNetCore.Mvc;
using OpenChatAgents.Api.Dtos;
using OpenChatAgents.Api.Services;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/sessions")]
public class SessionsController(SessionService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SessionResponse>> Create([FromBody] SessionCreate payload)
    {
        var session = await service.CreateSessionAsync(payload.Title, payload.AgentId);
        return StatusCode(StatusCodes.Status201Created, SessionResponse.FromEntity(session));
    }

    [HttpGet]
    public async Task<ActionResult<SessionListResponse>> List()
    {
        var sessions = await service.ListSessionsAsync();
        return Ok(new SessionListResponse
        {
            Sessions = [.. sessions.Select(SessionResponse.FromEntity)],
            Total = sessions.Count,
        });
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<SessionResponse>> Get(Guid sessionId)
    {
        var session = await service.GetSessionAsync(sessionId);
        return Ok(SessionResponse.FromEntity(session));
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> Delete(Guid sessionId)
    {
        await service.DeleteSessionAsync(sessionId);
        return NoContent();
    }
}
