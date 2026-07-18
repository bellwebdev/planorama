namespace Planorama.Api.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Allowed origins: the Cloudflare Pages origin plus localhost in dev.</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
