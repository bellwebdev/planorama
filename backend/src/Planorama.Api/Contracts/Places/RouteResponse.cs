using Planorama.Core.Integrations;

namespace Planorama.Api.Contracts.Places;

/// <param name="Geometry">Route geometry as a raw GeoJSON object, ready to hand to a map layer.</param>
public record RouteResponse(int DistanceMeters, int DurationSeconds, TravelMode Mode, string? Geometry)
{
    public static RouteResponse FromResult(RouteResult route, TravelMode mode) => new(
        route.DistanceMeters, (int)route.Duration.TotalSeconds, mode, route.PolylineGeoJson);
}
