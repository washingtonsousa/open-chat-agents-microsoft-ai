using System.ComponentModel.DataAnnotations;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Dtos;

public class UserCreate
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(4)]
    public string Password { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static UserResponse FromEntity(Models.User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        IsAdmin = user.IsAdmin,
        MustChangePassword = user.MustChangePassword,
        CreatedAt = user.CreatedAt,
    };
}

public class UserListResponse
{
    public List<UserResponse> Users { get; set; } = [];
    public int Total { get; set; }
}
