using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Core.Integrations;
using Planorama.Core.Places;

namespace Planorama.Api.Places;

/// <inheritdoc cref="IPlacesProvider"/>
/// <remarks>Geoapify's Places API is OpenStreetMap-derived: it carries no rating score, so every
/// result's <see cref="PlaceResult.Rating"/> is null. That field stays on the contract for a future
/// ratings-carrying provider.</remarks>
public class GeoapifyPlacesProvider(HttpClient httpClient, IOptions<GeoapifyOptions> options) : IPlacesProvider
{
    private readonly GeoapifyOptions _geoapify = options.Value;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlaceResult>> SearchNearbyAsync(PlaceSearchQuery query, CancellationToken ct)
    {
        string lat = GeoapifyClient.Coord(query.Origin.Latitude);
        string lon = GeoapifyClient.Coord(query.Origin.Longitude);

        var parameters = new Dictionary<string, string?>
        {
            ["categories"] = GeoapifyCategories.ToQueryValue(query.Category),
            ["filter"] = $"circle:{lon},{lat},{query.RadiusMeters}",
            ["bias"] = $"proximity:{lon},{lat}",
            ["limit"] = query.Limit.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(query.NameContains))
        {
            parameters["name"] = query.NameContains.Trim();
        }

        var response = await GeoapifyClient.GetAsync<PlacesResponse>(
            httpClient, $"{_geoapify.BaseUrl}/v2/places", parameters, _geoapify.ApiKey, ct);

        return response?.Features?
            .Select(f => f.Properties)
            // OSM carries plenty of unnamed nodes; a result nobody can identify isn't worth showing.
            .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Name) && !string.IsNullOrWhiteSpace(p.PlaceId))
            .Select(p => new PlaceResult(
                p!.PlaceId!,
                p.Name!,
                new GeoPoint(p.Lat, p.Lon),
                GeoapifyCategories.FromProvider(p.Categories, query.Category),
                p.Formatted,
                p.Distance is { } distance ? (int)Math.Round(distance) : null,
                Rating: null))
            .OrderBy(p => p.DistanceMeters ?? int.MaxValue)
            .ToList()
            ?? [];
    }

    /// <inheritdoc/>
    public async Task<PlaceDetail?> GetDetailAsync(string providerPlaceId, CancellationToken ct)
    {
        var parameters = new Dictionary<string, string?> { ["id"] = providerPlaceId };

        var response = await GeoapifyClient.GetAsync<PlaceDetailsResponse>(
            httpClient, $"{_geoapify.BaseUrl}/v2/place-details", parameters, _geoapify.ApiKey, ct);

        DetailProperties? properties = response?.Features?
            .Select(f => f.Properties)
            .FirstOrDefault(p => p is not null && !string.IsNullOrWhiteSpace(p.PlaceId));

        if (properties is null)
        {
            return null;
        }

        return new PlaceDetail(
            properties.PlaceId!,
            properties.Name ?? properties.AddressLine1 ?? "Unnamed place",
            new GeoPoint(properties.Lat, properties.Lon),
            properties.Categories is null ? null : GeoapifyCategories.FromProvider(properties.Categories, PlaceCategory.Attraction),
            properties.Formatted,
            properties.Datasource?.Raw?.Description,
            properties.Website,
            Rating: null);
    }

    private sealed record PlacesResponse(List<PlaceFeature>? Features);

    private sealed record PlaceFeature(PlaceProperties? Properties);

    private sealed record PlaceProperties(
        string? PlaceId, string? Name, string? Formatted, double Lat, double Lon, List<string>? Categories, double? Distance);

    private sealed record PlaceDetailsResponse(List<DetailFeature>? Features);

    private sealed record DetailFeature(DetailProperties? Properties);

    private sealed record DetailProperties(
        string? PlaceId, string? Name, string? Formatted, string? AddressLine1, double Lat, double Lon,
        List<string>? Categories, string? Website, Datasource? Datasource);

    private sealed record Datasource(RawTags? Raw);

    private sealed record RawTags(string? Description);
}
