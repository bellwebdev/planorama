namespace Planorama.Core.Options;

/// <summary>
/// Lives in Core (not Api, despite being JWT-specific) because <c>AuthService</c> needs
/// <see cref="RefreshTokenDays"/> to compute refresh-token expiry, and Core can't depend on Api.
/// Token issuance itself still happens in Api's <c>JwtAccessTokenIssuer</c>, which owns the JWT packages.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "planorama";
    public string Audience { get; set; } = "planorama";
    /// <summary>HMAC signing key; supplied via environment (Jwt__SigningKey), never committed.</summary>
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
