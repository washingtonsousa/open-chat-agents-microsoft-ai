using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using OpenChatAgents.Application.Dtos;
using OpenChatAgents.Application.Exceptions;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Options;
using OpenChatAgents.Domain.Repositories;
using Models = OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Application.Services;

public static class AuthClaimTypes
{
    public const string IsAdmin = "is_admin";
    public const string TokenUse = "token_use";
}

public class AuthService(IUserRepository userRepo, IConsumerApplicationRepository consumerAppRepo, IPasswordHasher hasher, IOptions<AppOptions> options)
{
    private readonly JwtOptions _jwt = options.Value.Jwt;

    public async Task<TokenResponse> IssueClientCredentialsTokenAsync(string clientId, string clientSecret)
    {
        var app = await consumerAppRepo.GetByClientIdAsync(clientId);
        if (app is null || !app.IsActive || !hasher.Verify(clientSecret, app.ClientSecretHash))
            throw ApiException.Unauthorized("client_id ou client_secret inválidos.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, app.Id.ToString()),
            new("client_id", app.ClientId),
            new(AuthClaimTypes.TokenUse, "client"),
        };

        var expiresIn = TimeSpan.FromMinutes(_jwt.ExpiryMinutes);
        return new TokenResponse
        {
            AccessToken = BuildToken(claims, expiresIn),
            TokenType = "Bearer",
            ExpiresIn = (int)expiresIn.TotalSeconds,
        };
    }

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

        return BuildToken(claims, TimeSpan.FromMinutes(_jwt.ExpiryMinutes));
    }

    private string BuildToken(IEnumerable<Claim> claims, TimeSpan expiresIn)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiresIn),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
