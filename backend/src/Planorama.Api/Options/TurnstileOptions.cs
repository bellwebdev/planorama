namespace Planorama.Api.Options;

public class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    /// <summary>Server-only — verified against Cloudflare's siteverify endpoint, never shipped to the client.</summary>
    public string SecretKey { get; set; } = string.Empty;
}
