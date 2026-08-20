namespace Planorama.Core.Integrations;

/// <summary>Turns a free-text address into coordinates. Separate from <see cref="IPlacesProvider"/>
/// because the two are independently swappable even when one vendor currently serves both.</summary>
public interface IGeocodingProvider
{
    /// <summary>Resolves an address to a coordinate.</summary>
    /// <param name="address">Free-text address or place name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The best match, or <c>null</c> when the address can't be resolved.</returns>
    Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct);
}

/// <param name="Timezone">IANA timezone id reported for the coordinate, when the provider supplies one.</param>
public record GeocodeResult(GeoPoint Location, string FormattedAddress, string? Timezone);
