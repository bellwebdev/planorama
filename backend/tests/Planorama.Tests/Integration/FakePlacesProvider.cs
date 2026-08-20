using Planorama.Core.Integrations;
using Planorama.Core.Places;

namespace Planorama.Tests.Integration;

/// <summary>Stands in for the Geoapify adapter so endpoint tests exercise authorisation and
/// mapping without a network call or a live API key.</summary>
public class FakePlacesProvider : IPlacesProvider
{
    /// <summary>Detail lookups for this id return null, simulating a place the provider doesn't know.</summary>
    public const string UnknownPlaceId = "unknown-place";

    public static PlaceResult SampleResult(PlaceCategory category) =>
        new("place-1", "Harbour Museum", new GeoPoint(51.5074, -0.1278), category, "1 Dock Rd", 420, Rating: null);

    public Task<IReadOnlyList<PlaceResult>> SearchNearbyAsync(PlaceSearchQuery query, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PlaceResult>>([SampleResult(query.Category)]);

    public Task<PlaceDetail?> GetDetailAsync(string providerPlaceId, CancellationToken ct) =>
        Task.FromResult(providerPlaceId == UnknownPlaceId
            ? null
            : new PlaceDetail(providerPlaceId, "Harbour Museum", new GeoPoint(51.5074, -0.1278),
                PlaceCategory.Museum, "1 Dock Rd", "A museum.", "https://example.test", Rating: null));
}
