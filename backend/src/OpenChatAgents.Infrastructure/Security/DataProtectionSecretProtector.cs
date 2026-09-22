using Microsoft.AspNetCore.DataProtection;
using OpenChatAgents.Domain.Abstractions;

namespace OpenChatAgents.Infrastructure.Security;

public class DataProtectionSecretProtector : ISecretProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("OpenChatAgents.McpServerSecrets");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
