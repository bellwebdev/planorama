namespace Planorama.Api.Options;

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
