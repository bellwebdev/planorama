namespace Planorama.Api.Options;

public class GeoapifyOptions
{
    public const string SectionName = "Geoapify";

    /// <summary>Server-only — appended to outbound provider calls, never returned to a client.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.geoapify.com";

    /// <summary>Free-tier daily credit allowance, shared by the Places, Routing and Geocoding APIs.</summary>
    public int DailyCreditCap { get; set; } = 3000;

    /// <summary>Fraction of <see cref="DailyCreditCap"/> at which spending stops, leaving headroom
    /// for the credit estimate being approximate.</summary>
    public double SoftCapFraction { get; set; } = 0.9;

    public int TimeoutSeconds { get; set; } = 10;
}
