using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using OpenChatAgents.Api.Dtos;
using OpenChatAgents.Api.Repositories;
using OpenChatAgents.Infrastructure.Options;
using OpenChatAgents.Infrastructure.Security;
using Models = OpenChatAgents.Infrastructure.Models;

namespace OpenChatAgents.Api.Services;

public static class AuthClaimTypes
{
    public const string IsAdmin = "is_admin";
}

public class AuthService(UserRepository userRepo, Argon2PasswordHasher hasher, IOptions<AppOptions> options)
{
    private readonly JwtOptions _jwt = options.Value.Jwt;

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var user = await userRepo.GetByUsernameAsync(username);
        if (user is null || !hasher.Verify(password, user.PasswordHash))
            throw ApiException.Unauthorized("Usuário ou senha inválidos.");

        return new LoginResponse
        {
            AccessToken = IssueToken(user),
            MustChangePassword = user.MustChangePassword,
            User = UserResponse.FromEntity(user),
        };
    }

    public async Task ChangePasswordAsync(Models.User user, string currentPassword, string newPassword)
    {
        if (!hasher.Verify(currentPassword, user.PasswordHash))
            throw ApiException.Conflict("Senha atual incorreta.");

        await userRepo.UpdatePasswordAsync(user, hasher.Hash(newPassword), mustChangePassword: false);
    }

    public string IssueToken(Models.User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(AuthClaimTypes.IsAdmin, user.IsAdmin ? "true" : "false"),
        };
        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
