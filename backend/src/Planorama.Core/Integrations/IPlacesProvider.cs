using Planorama.Core.Places;

namespace Planorama.Core.Integrations;

/// <summary>Read-only access to a third-party places catalogue. Implemented in the infrastructure
/// layer; business logic depends only on this interface so the provider stays swappable.</summary>
public interface IPlacesProvider
{
    /// <summary>Searches for places of one category near a point.</summary>
    /// <param name="query">The search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching places, nearest first; empty when nothing matches.</returns>
    /// <exception cref="Exceptions.ProviderQuotaExhaustedException">The daily provider quota is spent and the result wasn't cached.</exception>
    /// <exception cref="Exceptions.ProviderUnavailableException">The provider call failed or returned an error.</exception>
    Task<IReadOnlyList<PlaceResult>> SearchNearbyAsync(PlaceSearchQuery query, CancellationToken ct);

    /// <summary>Fetches the full detail record for a single place.</summary>
    /// <param name="providerPlaceId">The provider's own identifier, as returned by <see cref="SearchNearbyAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The place detail, or <c>null</c> if the provider has no such place.</returns>
    /// <exception cref="Exceptions.ProviderQuotaExhaustedException">The daily provider quota is spent and the result wasn't cached.</exception>
    /// <exception cref="Exceptions.ProviderUnavailableException">The provider call failed or returned an error.</exception>
    Task<PlaceDetail?> GetDetailAsync(string providerPlaceId, CancellationToken ct);
}

/// <param name="Rating">Always <c>null</c> on OpenStreetMap-derived providers such as Geoapify;
/// kept on the contract for a future ratings-carrying provider (Google Places, Phase 2+).</param>
public record PlaceResult(
    string ProviderPlaceId,
    string Name,
    GeoPoint Location,
    PlaceCategory Category,
    string? Address,
    int? DistanceMeters,
    decimal? Rating);

/// <inheritdoc cref="PlaceResult"/>
public record PlaceDetail(
    string ProviderPlaceId,
    string Name,
    GeoPoint Location,
    PlaceCategory? Category,
    string? Address,
    string? Description,
    string? Website,
    decimal? Rating);
