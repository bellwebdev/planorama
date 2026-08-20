using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Core.Integrations;

namespace Planorama.Api.Places;

/// <inheritdoc cref="IGeocodingProvider"/>
public class GeoapifyGeocodingProvider(HttpClient httpClient, IOptions<GeoapifyOptions> options) : IGeocodingProvider
{
    private readonly GeoapifyOptions _geoapify = options.Value;

    /// <inheritdoc/>
    public async Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["text"] = address,
            ["limit"] = "1",
            ["format"] = "json",
        };

        var response = await GeoapifyClient.GetAsync<GeocodeResponse>(
            httpClient, $"{_geoapify.BaseUrl}/v1/geocode/search", parameters, _geoapify.ApiKey, ct);

        GeocodeItem? best = response?.Results?.FirstOrDefault();
        return best is null
            ? null
            : new GeocodeResult(new GeoPoint(best.Lat, best.Lon), best.Formatted ?? address, best.Timezone?.Name);
    }

    private sealed record GeocodeResponse(List<GeocodeItem>? Results);

    private sealed record GeocodeItem(double Lat, double Lon, string? Formatted, GeocodeTimezone? Timezone);

    private sealed record GeocodeTimezone(string? Name);
}
