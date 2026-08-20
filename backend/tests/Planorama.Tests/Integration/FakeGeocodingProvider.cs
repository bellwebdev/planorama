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

    /// <summary>Every address this fake was asked to resolve, in call order — lets tests assert
    /// the stay address was disambiguated with the trip's destination rather than sent alone.</summary>
    public List<string> ReceivedAddresses { get; } = [];

    public Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct)
    {
        ReceivedAddresses.Add(address);
        return Task.FromResult(address.Contains(UnresolvableAddress, StringComparison.OrdinalIgnoreCase)
            ? null
            : new GeocodeResult(new GeoPoint(Latitude, Longitude), address, "Europe/London"));
    }
}
