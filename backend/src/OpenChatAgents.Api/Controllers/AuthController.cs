using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenChatAgents.Api.Dtos;
using OpenChatAgents.Api.Repositories;
using OpenChatAgents.Api.Services;

namespace OpenChatAgents.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService authService, UserRepository userRepo) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest payload)
    {
        var response = await authService.LoginAsync(payload.Username, payload.Password);
        return Ok(response);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest payload)
    {
        var user = await CurrentUserAsync();
        await authService.ChangePasswordAsync(user, payload.CurrentPassword, payload.NewPassword);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var user = await CurrentUserAsync();
        return Ok(UserResponse.FromEntity(user));
    }

    private async Task<Infrastructure.Models.User> CurrentUserAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var user = await userRepo.GetByIdAsync(userId);
        return user ?? throw ApiException.Unauthorized("Usuário não encontrado.");
    }
}
