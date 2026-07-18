namespace Planorama.Core.Integrations;

public interface IPlacesProvider
{
    Task<IReadOnlyList<PlaceResult>> SearchNearbyAsync(GeoPoint origin, string? category, int radiusMeters, CancellationToken ct);
    Task<PlaceDetail?> GetDetailAsync(string providerPlaceId, CancellationToken ct);
}

public record PlaceResult(string ProviderPlaceId, string Name, GeoPoint Location, string? Category, decimal? Rating);

public record PlaceDetail(string ProviderPlaceId, string Name, GeoPoint Location, string? Address, string? Description, decimal? Rating);
