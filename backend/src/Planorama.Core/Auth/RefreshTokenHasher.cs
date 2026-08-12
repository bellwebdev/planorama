using System.Security.Cryptography;
using System.Text;

namespace Planorama.Core.Auth;

/// <summary>Generates high-entropy refresh token secrets and hashes them for storage; the raw value is only ever returned to the caller once, never persisted.</summary>
public static class RefreshTokenHasher
{
    public static string GenerateRaw() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    // SHA-256, not bcrypt/argon2: this hashes a high-entropy random secret, not a low-entropy
    // password, so a slow hash buys no security here and would only add latency.
    public static string Hash(string rawToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
