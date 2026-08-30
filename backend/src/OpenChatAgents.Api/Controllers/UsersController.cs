using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenChatAgents.Api.Dtos;
using OpenChatAgents.Api.Services;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController(UserService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] UserCreate payload)
    {
        var createdByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var user = await service.CreateUserAsync(payload, createdByUserId);
        return StatusCode(StatusCodes.Status201Created, UserResponse.FromEntity(user));
    }

    [HttpGet]
    public async Task<ActionResult<UserListResponse>> List()
    {
        var users = await service.ListUsersAsync();
        return Ok(new UserListResponse
        {
            Users = [.. users.Select(UserResponse.FromEntity)],
            Total = users.Count,
        });
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId)
    {
        await service.DeleteUserAsync(userId);
        return NoContent();
    }
}
