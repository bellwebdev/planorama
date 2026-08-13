namespace Planorama.Api.Options;

public class GoogleOptions
{
    public const string SectionName = "Google";

    /// <summary>The OAuth client ID — checked as the expected audience on every ID token.</summary>
    public string ClientId { get; set; } = string.Empty;
}
