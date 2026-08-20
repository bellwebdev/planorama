using Planorama.Core.Integrations;

namespace Planorama.Core.Places;

/// <summary>Trip-scoped place discovery: resolves and authorises the trip, then delegates to the
/// provider abstractions.</summary>
public interface IPlaceService
{
    /// <summary>Searches for places near the trip's stay address.</summary>
    /// <exception cref="Exceptions.TripNotFoundException">The trip doesn't exist, or the caller isn't an accepted member.</exception>
    /// <exception cref="Exceptions.TripNotGeocodedException">The trip's stay address has no resolved coordinate.</exception>
    Task<IReadOnlyList<PlaceResult>> SearchNearStayAsync(
        Guid tripId, Guid userId, PlaceCategory category, int radiusMeters, string? nameContains, int limit, CancellationToken ct);

    /// <summary>Fetches one place's detail record.</summary>
    /// <exception cref="Exceptions.PlaceNotFoundException">No such place at the provider.</exception>
    Task<PlaceDetail> GetDetailAsync(string providerPlaceId, CancellationToken ct);

    /// <summary>Routes from the trip's stay address to an arbitrary destination.</summary>
    /// <exception cref="Exceptions.TripNotFoundException">The trip doesn't exist, or the caller isn't an accepted member.</exception>
    /// <exception cref="Exceptions.TripNotGeocodedException">The trip's stay address has no resolved coordinate.</exception>
    Task<RouteResult> GetRouteFromStayAsync(Guid tripId, Guid userId, GeoPoint destination, TravelMode mode, CancellationToken ct);
}
