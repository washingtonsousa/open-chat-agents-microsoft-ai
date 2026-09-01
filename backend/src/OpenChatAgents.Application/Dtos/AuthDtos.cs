using System.ComponentModel.DataAnnotations;

namespace OpenChatAgents.Application.Dtos;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public UserResponse User { get; set; } = null!;
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(4)]
    public string NewPassword { get; set; } = string.Empty;
}
