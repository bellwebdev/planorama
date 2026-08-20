using Planorama.Core.Integrations;

namespace Planorama.Tests.Integration;

/// <inheritdoc cref="FakePlacesProvider"/>
public class FakeGeocodingProvider : IGeocodingProvider
{
    /// <summary>Any address containing this sentinel fails to resolve, so tests can produce a trip
    /// with no stay coordinate — the state that makes place search return 409.</summary>
    public const string UnresolvableAddress = "nowhere";

    public const double Latitude = 51.5074;
    public const double Longitude = -0.1278;

    public Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct) =>
        Task.FromResult(address.Contains(UnresolvableAddress, StringComparison.OrdinalIgnoreCase)
            ? null
            : new GeocodeResult(new GeoPoint(Latitude, Longitude), address, "Europe/London"));
}
