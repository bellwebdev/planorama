using Planorama.Core.Integrations;

namespace Planorama.Tests.Integration;

/// <inheritdoc cref="FakePlacesProvider"/>
public class FakeRoutingProvider : IRoutingProvider
{
    public const int DistanceMeters = 8_400;
    public const int DurationSeconds = 960;

    public Task<RouteResult> GetRouteAsync(GeoPoint from, GeoPoint to, TravelMode mode, CancellationToken ct) =>
        Task.FromResult(new RouteResult(
            DistanceMeters,
            TimeSpan.FromSeconds(DurationSeconds),
            """{"type":"LineString","coordinates":[[0,0],[1,1]]}"""));
}
