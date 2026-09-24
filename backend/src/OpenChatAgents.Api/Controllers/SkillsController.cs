using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Services;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/skills")]
public class SkillsController(SkillService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SkillResponse>> Create([FromBody] SkillCreate payload)
    {
        var skill = await service.CreateAsync(payload, CurrentUserId());
        return StatusCode(StatusCodes.Status201Created, SkillResponse.FromEntity(skill));
    }

    [HttpGet]
    public async Task<ActionResult<SkillListResponse>> List()
    {
        var skills = await service.ListAsync();
        return Ok(new SkillListResponse
        {
            Skills = [.. skills.Select(SkillResponse.FromEntity)],
            Total = skills.Count,
        });
    }

    [HttpGet("{skillId:guid}")]
    public async Task<ActionResult<SkillResponse>> Get(Guid skillId)
    {
        var skill = await service.GetAsync(skillId);
        return Ok(SkillResponse.FromEntity(skill));
    }

    [HttpPut("{skillId:guid}")]
    public async Task<ActionResult<SkillResponse>> Update(Guid skillId, [FromBody] SkillUpdate payload)
    {
        var skill = await service.UpdateAsync(skillId, payload);
        return Ok(SkillResponse.FromEntity(skill));
    }

    [HttpDelete("{skillId:guid}")]
    public async Task<IActionResult> Delete(Guid skillId)
    {
        await service.DeleteAsync(skillId);
        return NoContent();
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
