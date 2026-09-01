using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Options;

namespace OpenChatAgents.Infrastructure.Security;

public class Argon2PasswordHasher(IOptions<AppOptions> options) : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private readonly Argon2Options _options = options.Value.Argon2;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt, _options.MemoryKb, _options.Iterations, _options.Parallelism);
        return Encode(hash, salt, _options.MemoryKb, _options.Iterations, _options.Parallelism);
    }

    public bool Verify(string password, string encodedHash)
    {
        if (!TryDecode(encodedHash, out var hash, out var salt, out var memoryKb, out var iterations, out var parallelism))
            return false;

        var computed = ComputeHash(password, salt, memoryKb, iterations, parallelism);
        return CryptographicOperations.FixedTimeEquals(computed, hash);
    }

    private static byte[] ComputeHash(string password, byte[] salt, int memoryKb, int iterations, int parallelism)
    {
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            MemorySize = memoryKb,
            Iterations = iterations,
        };
        return argon2.GetBytes(HashSize);
    }

    private static string Encode(byte[] hash, byte[] salt, int memoryKb, int iterations, int parallelism) =>
        $"$argon2id$v=19$m={memoryKb},t={iterations},p={parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

    private static bool TryDecode(string encoded, out byte[] hash, out byte[] salt, out int memoryKb, out int iterations, out int parallelism)
    {
        hash = []; salt = []; memoryKb = 0; iterations = 0; parallelism = 0;

        var parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 || parts[0] != "argon2id")
            return false;

        var paramParts = parts[2].Split(',');
        foreach (var p in paramParts)
        {
            var kv = p.Split('=');
            if (kv.Length != 2) return false;
            switch (kv[0])
            {
                case "m": memoryKb = int.Parse(kv[1]); break;
                case "t": iterations = int.Parse(kv[1]); break;
                case "p": parallelism = int.Parse(kv[1]); break;
            }
        }

        try
        {
            salt = Convert.FromBase64String(parts[3]);
            hash = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        return memoryKb > 0 && iterations > 0 && parallelism > 0;
    }
}
