using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Planorama.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the <c>sub</c> claim set by <see cref="JwtAccessTokenIssuer"/>. Only ever called on an
    /// already-authorized request, so a missing/malformed claim indicates a broken invariant (a
    /// token-issuance bug), not a user-facing failure — hence a plain exception, not an
    /// <see cref="Planorama.Core.Exceptions.AuthProblemException"/>; it falls through to the
    /// default 500 handling.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Authenticated principal is missing a 'sub' claim.");

        return Guid.TryParse(sub, out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated principal's 'sub' claim is not a valid GUID.");
    }
}
