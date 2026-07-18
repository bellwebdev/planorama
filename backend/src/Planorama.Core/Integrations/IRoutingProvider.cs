namespace Planorama.Core.Integrations;

public interface IRoutingProvider
{
    Task<RouteResult> GetRouteAsync(GeoPoint from, GeoPoint to, TravelMode mode, CancellationToken ct);
}

public enum TravelMode
{
    Drive,
    Walk,
    Bicycle,
    Transit,
}

public record RouteResult(int DistanceMeters, TimeSpan Duration, string? PolylineGeoJson);
