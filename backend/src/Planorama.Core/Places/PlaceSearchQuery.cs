using Planorama.Core.Integrations;

namespace Planorama.Core.Places;

/// <summary>A nearby-places search, bundled as one value so cache keys and provider calls can't
/// drift apart as parameters are added.</summary>
/// <param name="Origin">Centre of the search — the trip's stay point.</param>
/// <param name="Category">The single category to search for; providers require at least one.</param>
/// <param name="RadiusMeters">Search radius around <paramref name="Origin"/>.</param>
/// <param name="NameContains">Optional name filter; <c>null</c> browses the whole category.</param>
/// <param name="Limit">Maximum results to return.</param>
public record PlaceSearchQuery(
    GeoPoint Origin,
    PlaceCategory Category,
    int RadiusMeters,
    string? NameContains,
    int Limit)
{
    /// <summary>Stable cache key fragment: identical searches from anywhere in the same ~5km cell
    /// share one cached provider response.</summary>
    public string CacheFragment =>
        $"{Geohash.Encode(Origin)}:{Category}:{RadiusMeters}:{NameContains?.Trim().ToLowerInvariant() ?? "-"}:{Limit}";
}
