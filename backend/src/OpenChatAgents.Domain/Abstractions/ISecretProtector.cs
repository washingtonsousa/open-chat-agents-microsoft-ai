namespace OpenChatAgents.Domain.Abstractions;

/// <summary>Encrypts/decrypts secrets that must be recoverable in plaintext (unlike passwords, which are only hashed).</summary>
public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
