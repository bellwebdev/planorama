using System.Security.Cryptography;

namespace Planorama.Core.Suggestions;

/// <inheritdoc/>
public class CryptoCoinFlip : ICoinFlip
{
    /// <inheritdoc/>
    public bool FlipApproved() => RandomNumberGenerator.GetInt32(2) == 1;
}
