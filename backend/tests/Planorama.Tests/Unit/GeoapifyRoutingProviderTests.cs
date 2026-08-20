using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Api.Places;
using Planorama.Core.Exceptions;
using Planorama.Core.Integrations;
using Xunit;

namespace Planorama.Tests.Unit;

public class GeoapifyRoutingProviderTests
{
    [Theory]
    [InlineData("meters", 8400, 8400)]
    [InlineData("kilometers", 8.4, 8400)]
    [InlineData("miles", 5.2, 8369)]
    public async Task Converts_distance_from_the_unit_the_provider_reports(string units, double distance, int expectedMeters)
    {
        string body = $$"""
        { "features": [ { "properties": { "distance": {{distance}}, "distance_units": "{{units}}", "time": 960 },
                          "geometry": { "type": "LineString", "coordinates": [[0,0],[1,1]] } } ] }
        """;

        RouteResult route = await CreateProvider(body).GetRouteAsync(
            new GeoPoint(51.5, -0.12), new GeoPoint(51.6, -0.1), TravelMode.Drive, default);

        Assert.Equal(expectedMeters, route.DistanceMeters);
        Assert.Equal(TimeSpan.FromSeconds(960), route.Duration);
        Assert.Contains("LineString", route.PolylineGeoJson);
    }

    [Fact]
    public async Task Throws_RouteNotFound_when_the_provider_returns_no_route()
    {
        GeoapifyRoutingProvider provider = CreateProvider("""{"features":[]}""");

        await Assert.ThrowsAsync<RouteNotFoundException>(
            () => provider.GetRouteAsync(new GeoPoint(51.5, -0.12), new GeoPoint(51.6, -0.1), TravelMode.Walk, default));
    }

    private static GeoapifyRoutingProvider CreateProvider(string body) =>
        new(new HttpClient(new StubHttpMessageHandler(System.Net.HttpStatusCode.OK, body)),
            Options.Create(new GeoapifyOptions { ApiKey = "test-key" }));
}
