using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Planorama.Api.Contracts.Places;
using Planorama.Api.Contracts.Trips;
using Planorama.Core.Integrations;
using Planorama.Core.Places;
using Xunit;

namespace Planorama.Tests.Integration;

[Collection("Api")]
public class PlaceEndpointsTests(PlanoramaWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Search_returns_places_near_the_trip_stay_for_a_member()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);

        var response = await _client.AuthenticatedGetAsync(
            $"/api/v1/trips/{trip.Id}/places/search?category=Museum&radius=3000", login.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var places = await response.Content.ReadFromJsonAsync<List<PlaceResponse>>(JsonOptions);
        PlaceResponse place = Assert.Single(places!);
        Assert.Equal("Harbour Museum", place.Name);
        Assert.Equal(PlaceCategory.Museum, place.Category);
        // OpenStreetMap-derived data carries no rating; the field stays on the contract regardless.
        Assert.Null(place.Rating);
    }

    [Fact]
    public async Task Search_returns_404_for_a_non_member()
    {
        var owner = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var outsider = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(owner.Tokens.AccessToken);

        var response = await _client.AuthenticatedGetAsync(
            $"/api/v1/trips/{trip.Id}/places/search?category=Museum", outsider.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Search_requires_authentication()
    {
        var response = await _client.GetAsync($"/api/v1/trips/{Guid.NewGuid()}/places/search?category=Museum");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_returns_409_when_the_stay_address_could_not_be_geocoded()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken, stayAddress: FakeGeocodingProvider.UnresolvableAddress);

        Assert.Null(trip.StayLat);

        var response = await _client.AuthenticatedGetAsync(
            $"/api/v1/trips/{trip.Id}/places/search?category=Museum", login.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("category=Museum&radius=10")]
    [InlineData("category=Museum&radius=999999")]
    [InlineData("category=Museum&limit=500")]
    [InlineData("category=NotACategory")]
    [InlineData("")]
    public async Task Search_rejects_out_of_range_parameters(string queryString)
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);

        var response = await _client.AuthenticatedGetAsync(
            $"/api/v1/trips/{trip.Id}/places/search?{queryString}", login.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Route_returns_distance_and_duration_from_the_stay()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);

        var response = await _client.AuthenticatedGetAsync(
            $"/api/v1/trips/{trip.Id}/route?toLat=51.51&toLng=-0.13&mode=Walk", login.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var route = await response.Content.ReadFromJsonAsync<RouteResponse>(JsonOptions);
        Assert.Equal(FakeRoutingProvider.DistanceMeters, route!.DistanceMeters);
        Assert.Equal(FakeRoutingProvider.DurationSeconds, route.DurationSeconds);
        Assert.Contains("LineString", route.Geometry);
    }

    [Fact]
    public async Task Route_returns_404_for_a_non_member()
    {
        var owner = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var outsider = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(owner.Tokens.AccessToken);

        var response = await _client.AuthenticatedGetAsync(
            $"/api/v1/trips/{trip.Id}/route?toLat=51.51&toLng=-0.13", outsider.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Route_rejects_an_out_of_range_destination()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);

        var response = await _client.AuthenticatedGetAsync(
            $"/api/v1/trips/{trip.Id}/route?toLat=200&toLng=-0.13", login.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Place_detail_returns_the_place_and_404_for_an_unknown_id()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var found = await _client.AuthenticatedGetAsync("/api/v1/places/place-1", login.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        var detail = await found.Content.ReadFromJsonAsync<PlaceDetailResponse>(JsonOptions);
        Assert.Equal("place-1", detail!.ProviderPlaceId);

        var missing = await _client.AuthenticatedGetAsync(
            $"/api/v1/places/{FakePlacesProvider.UnknownPlaceId}", login.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Categories_returns_the_catalog_to_authenticated_callers_only()
    {
        var anonymous = await _client.GetAsync("/api/v1/places/categories");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var response = await _client.AuthenticatedGetAsync("/api/v1/places/categories", login.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<PlaceCategoryResponse>>(JsonOptions);
        Assert.Equal(Enum.GetValues<PlaceCategory>().Length, categories!.Count);
        Assert.Contains(categories, c => c.Value == PlaceCategory.Playground);
    }

    [Fact]
    public async Task Creating_a_trip_resolves_its_stay_coordinates()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);

        Assert.Equal(FakeGeocodingProvider.Latitude, trip.StayLat);
        Assert.Equal(FakeGeocodingProvider.Longitude, trip.StayLng);
    }

    [Fact]
    public async Task Creating_a_trip_geocodes_the_stay_address_with_the_destination_for_context()
    {
        var geocoder = (FakeGeocodingProvider)factory.Services.GetRequiredService<IGeocodingProvider>();
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        await CreateTripAsync(login.Tokens.AccessToken, stayAddress: "113A 81st Street");

        // A bare street address is ambiguous across cities — it must be geocoded together with
        // the trip's destination, not on its own, or it resolves to the wrong city entirely.
        Assert.Contains("113A 81st Street, Lake Tahoe", geocoder.ReceivedAddresses);
    }

    [Fact]
    public async Task Updating_only_the_destination_re_geocodes_the_stay_point()
    {
        var geocoder = (FakeGeocodingProvider)factory.Services.GetRequiredService<IGeocodingProvider>();
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken, stayAddress: "113A 81st Street");
        geocoder.ReceivedAddresses.Clear();

        var update = new UpdateTripRequest(
            trip.Name, trip.Description, "Sea Isle City, NJ", trip.StayAddress,
            trip.StartDate, trip.EndDate, trip.Timezone, trip.DefaultVotingWindowHours, trip.Status);
        await _client.AuthenticatedPatchAsync($"/api/v1/trips/{trip.Id}", login.Tokens.AccessToken, update);

        Assert.Contains("113A 81st Street, Sea Isle City, NJ", geocoder.ReceivedAddresses);
    }

    private async Task<TripResponse> CreateTripAsync(string accessToken, string stayAddress = "123 Shore Rd")
    {
        var request = new CreateTripRequest(
            "Lake Trip", null, "Lake Tahoe", stayAddress,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5), "America/Los_Angeles", 48);

        var response = await _client.AuthenticatedPostAsync("/api/v1/trips", accessToken, request, Guid.NewGuid().ToString());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TripResponse>(JsonOptions))!;
    }
}
