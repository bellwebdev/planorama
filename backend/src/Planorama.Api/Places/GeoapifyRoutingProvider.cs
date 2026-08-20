using System.Text.Json;
using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Core.Exceptions;
using Planorama.Core.Integrations;

namespace Planorama.Api.Places;

/// <inheritdoc cref="IRoutingProvider"/>
public class GeoapifyRoutingProvider(HttpClient httpClient, IOptions<GeoapifyOptions> options) : IRoutingProvider
{
    private readonly GeoapifyOptions _geoapify = options.Value;

    /// <inheritdoc/>
    public async Task<RouteResult> GetRouteAsync(GeoPoint from, GeoPoint to, TravelMode mode, CancellationToken ct)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["waypoints"] =
                $"{GeoapifyClient.Coord(from.Latitude)},{GeoapifyClient.Coord(from.Longitude)}|" +
                $"{GeoapifyClient.Coord(to.Latitude)},{GeoapifyClient.Coord(to.Longitude)}",
            ["mode"] = mode.ToString().ToLowerInvariant(),
            ["units"] = "metric",
        };

        var response = await GeoapifyClient.GetAsync<RoutingResponse>(
            httpClient, $"{_geoapify.BaseUrl}/v1/routing", parameters, _geoapify.ApiKey, ct);

        RouteFeature? route = response?.Features?.FirstOrDefault(f => f.Properties is not null);
        if (route?.Properties is not { } properties)
        {
            throw new RouteNotFoundException();
        }

        return new RouteResult(
            ToMeters(properties.Distance, properties.DistanceUnits),
            TimeSpan.FromSeconds(properties.Time),
            route.Geometry?.GetRawText());
    }

    /// <summary>Geoapify reports distance in whichever unit the request asked for and echoes that
    /// unit back; converting from the echoed unit keeps the result correct even if the default
    /// changes upstream.</summary>
    private static int ToMeters(double distance, string? units) => units?.ToLowerInvariant() switch
    {
        "kilometers" or "km" => (int)Math.Round(distance * 1000),
        "miles" or "mi" => (int)Math.Round(distance * 1609.344),
        _ => (int)Math.Round(distance),
    };

    private sealed record RoutingResponse(List<RouteFeature>? Features);

    private sealed record RouteFeature(RouteProperties? Properties, JsonElement? Geometry);

    private sealed record RouteProperties(double Distance, string? DistanceUnits, double Time);
}
