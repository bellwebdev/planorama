namespace Planorama.Core.Integrations;

/// <summary>Point-to-point directions from a third-party routing service.</summary>
public interface IRoutingProvider
{
    /// <summary>Calculates a route between two points.</summary>
    /// <param name="from">Origin — the trip's stay point.</param>
    /// <param name="to">Destination.</param>
    /// <param name="mode">How the traveller is getting there.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Distance, duration and geometry for the route.</returns>
    /// <exception cref="Exceptions.ProviderQuotaExhaustedException">The daily provider quota is spent and the result wasn't cached.</exception>
    /// <exception cref="Exceptions.ProviderUnavailableException">The provider call failed, or no route exists between the points.</exception>
    Task<RouteResult> GetRouteAsync(GeoPoint from, GeoPoint to, TravelMode mode, CancellationToken ct);
}

public enum TravelMode
{
    Drive,
    Walk,
    Bicycle,
    Transit,
}

/// <param name="PolylineGeoJson">Route geometry as a GeoJSON LineString, for drawing on the map.</param>
public record RouteResult(int DistanceMeters, TimeSpan Duration, string? PolylineGeoJson);
