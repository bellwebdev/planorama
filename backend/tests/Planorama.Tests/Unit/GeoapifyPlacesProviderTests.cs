using System.Net;
using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Api.Places;
using Planorama.Core.Integrations;
using Planorama.Core.Places;
using Xunit;

namespace Planorama.Tests.Unit;

public class GeoapifyPlacesProviderTests
{
    private const string SearchBody = """
    {
      "type": "FeatureCollection",
      "features": [
        { "properties": { "place_id": "abc", "name": "Harbour Museum", "formatted": "1 Dock Rd, London",
                          "lat": 51.5074, "lon": -0.1278, "categories": ["entertainment.museum"], "distance": 412.7 } },
        { "properties": { "place_id": "def", "name": "Old Fort", "formatted": "2 Fort Ln",
                          "lat": 51.51, "lon": -0.12, "categories": ["tourism.sights"], "distance": 120.2 } },
        { "properties": { "place_id": "ghi", "lat": 51.52, "lon": -0.11, "categories": ["entertainment.museum"] } }
      ]
    }
    """;

    [Fact]
    public async Task Maps_search_results_and_orders_them_by_distance()
    {
        (GeoapifyPlacesProvider provider, StubHttpMessageHandler _) = CreateProvider(SearchBody);

        IReadOnlyList<PlaceResult> results = await provider.SearchNearbyAsync(Query(), default);

        Assert.Equal(2, results.Count);
        Assert.Equal("Old Fort", results[0].Name);
        Assert.Equal(120, results[0].DistanceMeters);

        PlaceResult museum = results[1];
        Assert.Equal("abc", museum.ProviderPlaceId);
        Assert.Equal(PlaceCategory.Museum, museum.Category);
        Assert.Equal("1 Dock Rd, London", museum.Address);
        Assert.Equal(413, museum.DistanceMeters);
        Assert.Equal(51.5074, museum.Location.Latitude);
        Assert.Null(museum.Rating);
    }

    [Fact]
    public async Task Drops_unnamed_results()
    {
        (GeoapifyPlacesProvider provider, StubHttpMessageHandler _) = CreateProvider(SearchBody);

        IReadOnlyList<PlaceResult> results = await provider.SearchNearbyAsync(Query(), default);

        Assert.DoesNotContain(results, r => r.ProviderPlaceId == "ghi");
    }

    [Fact]
    public async Task Builds_a_circle_filter_with_invariant_coordinates()
    {
        (GeoapifyPlacesProvider provider, StubHttpMessageHandler handler) = CreateProvider(SearchBody);

        await provider.SearchNearbyAsync(Query(), default);

        Assert.Contains("filter=circle:-0.1278,51.5074,3000", handler.LastQuery);
        Assert.Contains("categories=entertainment.museum", handler.LastQuery);
        Assert.Contains("apiKey=test-key", handler.LastQuery);
    }

    [Fact]
    public async Task Omits_the_name_filter_when_no_search_text_is_given()
    {
        (GeoapifyPlacesProvider provider, StubHttpMessageHandler handler) = CreateProvider(SearchBody);

        await provider.SearchNearbyAsync(Query(), default);

        Assert.DoesNotContain("name=", handler.LastQuery);
    }

    [Fact]
    public async Task Returns_empty_when_the_provider_has_no_matches()
    {
        (GeoapifyPlacesProvider provider, StubHttpMessageHandler _) = CreateProvider("""{"type":"FeatureCollection","features":[]}""");

        Assert.Empty(await provider.SearchNearbyAsync(Query(), default));
    }

    [Fact]
    public async Task Surfaces_a_provider_error_as_ProviderUnavailable()
    {
        (GeoapifyPlacesProvider provider, StubHttpMessageHandler _) = CreateProvider("{}", HttpStatusCode.TooManyRequests);

        await Assert.ThrowsAsync<Core.Exceptions.ProviderUnavailableException>(
            () => provider.SearchNearbyAsync(Query(), default));
    }

    [Fact]
    public async Task Returns_null_detail_for_an_empty_feature_collection()
    {
        (GeoapifyPlacesProvider provider, StubHttpMessageHandler _) = CreateProvider("""{"type":"FeatureCollection","features":[]}""");

        Assert.Null(await provider.GetDetailAsync("abc", default));
    }

    [Fact]
    public async Task Maps_place_detail()
    {
        const string body = """
        {
          "features": [
            { "properties": { "place_id": "abc", "name": "Harbour Museum", "formatted": "1 Dock Rd",
                              "lat": 51.5, "lon": -0.12, "categories": ["entertainment.museum"],
                              "website": "https://museum.test",
                              "datasource": { "raw": { "description": "Maritime history." } } } }
          ]
        }
        """;
        (GeoapifyPlacesProvider provider, StubHttpMessageHandler _) = CreateProvider(body);

        PlaceDetail? detail = await provider.GetDetailAsync("abc", default);

        Assert.NotNull(detail);
        Assert.Equal("Harbour Museum", detail!.Name);
        Assert.Equal(PlaceCategory.Museum, detail.Category);
        Assert.Equal("https://museum.test", detail.Website);
        Assert.Equal("Maritime history.", detail.Description);
    }

    private static PlaceSearchQuery Query() =>
        new(new GeoPoint(51.5074, -0.1278), PlaceCategory.Museum, 3000, NameContains: null, Limit: 20);

    private static (GeoapifyPlacesProvider Provider, StubHttpMessageHandler Handler) CreateProvider(
        string body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new StubHttpMessageHandler(statusCode, body);
        var options = Options.Create(new GeoapifyOptions { ApiKey = "test-key" });
        return (new GeoapifyPlacesProvider(new HttpClient(handler), options), handler);
    }
}
