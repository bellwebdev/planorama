using Microsoft.EntityFrameworkCore;
using Planorama.Core.Data;
using Planorama.Core.Exceptions;
using Planorama.Core.Integrations;
using Planorama.Core.Trips;

namespace Planorama.Core.Places;

/// <inheritdoc cref="IPlaceService"/>
public class PlaceService(PlanoramaDbContext db, IPlacesProvider places, IRoutingProvider routing) : IPlaceService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlaceResult>> SearchNearStayAsync(
        Guid tripId, Guid userId, PlaceCategory category, int radiusMeters, string? nameContains, int limit, CancellationToken ct)
    {
        GeoPoint origin = await GetStayPointAsync(tripId, userId, ct);
        return await places.SearchNearbyAsync(new PlaceSearchQuery(origin, category, radiusMeters, nameContains, limit), ct);
    }

    /// <inheritdoc/>
    public async Task<PlaceDetail> GetDetailAsync(string providerPlaceId, CancellationToken ct) =>
        await places.GetDetailAsync(providerPlaceId, ct) ?? throw new PlaceNotFoundException();

    /// <inheritdoc/>
    public async Task<RouteResult> GetRouteFromStayAsync(
        Guid tripId, Guid userId, GeoPoint destination, TravelMode mode, CancellationToken ct)
    {
        GeoPoint origin = await GetStayPointAsync(tripId, userId, ct);
        return await routing.GetRouteAsync(origin, destination, mode, ct);
    }

    /// <summary>Projects to the two coordinates rather than materialising the trip — this runs on
    /// every search, and nothing else about the trip is needed.</summary>
    private async Task<GeoPoint> GetStayPointAsync(Guid tripId, Guid userId, CancellationToken ct)
    {
        var stay = await db.Trips
            .AsNoTracking()
            .AccessibleBy(userId)
            .Where(t => t.Id == tripId)
            .Select(t => new { t.StayLat, t.StayLng })
            .FirstOrDefaultAsync(ct)
            ?? throw new TripNotFoundException();

        if (stay.StayLat is not { } lat || stay.StayLng is not { } lng)
        {
            throw new TripNotGeocodedException();
        }

        return new GeoPoint(lat, lng);
    }
}
