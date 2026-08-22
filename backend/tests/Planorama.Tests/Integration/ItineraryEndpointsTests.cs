using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Planorama.Api.Contracts.Auth;
using Planorama.Api.Contracts.Itinerary;
using Planorama.Api.Contracts.Suggestions;
using Planorama.Api.Contracts.Trips;
using Planorama.Core.Domain;
using Xunit;

namespace Planorama.Tests.Integration;

[Collection("Api")]
public class ItineraryEndpointsTests(PlanoramaWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Approving_a_scheduled_suggestion_places_it_on_the_itinerary()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken,
            new CreateSuggestionRequest(null, "Scheduled kayaking", null, null, new DateOnly(2026, 9, 2), new TimeOnly(9, 0), 90, null));

        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);

        List<ItineraryItemResponse> items = await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken);
        ItineraryItemResponse item = Assert.Single(items);
        Assert.Equal(suggestion.Id, item.SuggestionId);
        Assert.Equal("Scheduled kayaking", item.Title);
        Assert.Equal(new DateOnly(2026, 9, 2), item.Date);
        Assert.Equal(new TimeOnly(9, 0), item.StartTime);
        Assert.Equal(new TimeOnly(10, 30), item.EndTime);
    }

    [Fact]
    public async Task Approving_an_unscheduled_suggestion_lands_it_in_the_unscheduled_tray()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Unscheduled golf", null, null, null, null, null, null));

        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);

        List<ItineraryItemResponse> items = await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken);
        ItineraryItemResponse item = Assert.Single(items);
        Assert.Null(item.Date);
        Assert.Null(item.StartTime);
    }

    [Fact]
    public async Task Vetoing_an_approved_suggestion_removes_it_from_the_itinerary()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Reversed golf", null, null, null, null, null, null));

        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);
        Assert.Single(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));

        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: false);
        Assert.Empty(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));
    }

    [Fact]
    public async Task Scheduled_items_are_listed_before_the_unscheduled_tray()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse unscheduled = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Unscheduled order golf", null, null, null, null, null, null));
        SuggestionResponse scheduled = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken,
            new CreateSuggestionRequest(null, "Scheduled order golf", null, null, new DateOnly(2026, 9, 2), new TimeOnly(9, 0), 60, null));

        await OverrideAsync(unscheduled.Id, owner.Tokens.AccessToken, approved: true);
        await OverrideAsync(scheduled.Id, owner.Tokens.AccessToken, approved: true);

        List<ItineraryItemResponse> items = await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken);
        Assert.Equal(2, items.Count);
        Assert.Equal("Scheduled order golf", items[0].Title);
        Assert.Equal("Unscheduled order golf", items[1].Title);
    }

    [Fact]
    public async Task Creator_can_reschedule_an_item()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Reschedule golf", null, null, null, null, null, null));
        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);
        ItineraryItemResponse item = Assert.Single(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));

        var request = new UpdateItineraryItemRequest(new DateOnly(2026, 9, 3), new TimeOnly(14, 0), new TimeOnly(15, 0), 5, "America/Los_Angeles");
        var response = await _client.AuthenticatedPatchAsync($"/api/v1/itinerary-items/{item.Id}", owner.Tokens.AccessToken, request);
        response.EnsureSuccessStatusCode();
        ItineraryItemResponse updated = (await response.Content.ReadFromJsonAsync<ItineraryItemResponse>(JsonOptions))!;

        Assert.Equal(new DateOnly(2026, 9, 3), updated.Date);
        Assert.Equal(new TimeOnly(14, 0), updated.StartTime);
        Assert.Equal(5, updated.SortOrder);
        Assert.Equal("America/Los_Angeles", updated.Timezone);
    }

    [Fact]
    public async Task A_non_creator_member_cannot_reschedule_an_item()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Forbidden reschedule golf", null, null, null, null, null, null));
        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);
        ItineraryItemResponse item = Assert.Single(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));

        var request = new UpdateItineraryItemRequest(null, null, null, 0, null);
        var response = await _client.AuthenticatedPatchAsync($"/api/v1/itinerary-items/{item.Id}", member.Tokens.AccessToken, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_non_member_cannot_list_the_itinerary()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        var outsider = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var response = await _client.AuthenticatedGetAsync($"/api/v1/trips/{trip.Id}/itinerary", outsider.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<SuggestionResponse> OverrideAsync(Guid suggestionId, string accessToken, bool approved)
    {
        var response = await _client.AuthenticatedPutAsync(
            $"/api/v1/suggestions/{suggestionId}/override", accessToken, new OverrideSuggestionRequest(approved));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SuggestionResponse>(JsonOptions))!;
    }

    private async Task<List<ItineraryItemResponse>> GetItineraryAsync(Guid tripId, string accessToken)
    {
        var response = await _client.AuthenticatedGetAsync($"/api/v1/trips/{tripId}/itinerary", accessToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ItineraryItemResponse>>(JsonOptions))!;
    }

    private async Task<(LoginResponse Owner, LoginResponse Member, TripResponse Trip)> CreateTripWithTwoMembersAsync()
    {
        var owner = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var member = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(owner.Tokens.AccessToken);

        var inviteResponse = await _client.AuthenticatedPostAsync(
            $"/api/v1/trips/{trip.Id}/invites", owner.Tokens.AccessToken, new CreateInviteRequest(InvitedVia.Link, null), Guid.NewGuid().ToString());
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(JsonOptions);

        var accept = await _client.AuthenticatedPostAsync($"/api/v1/invites/{invite!.Token}/accept", member.Tokens.AccessToken);
        accept.EnsureSuccessStatusCode();

        return (owner, member, trip);
    }

    private async Task<SuggestionResponse> CreateSuggestionAsync(Guid tripId, string accessToken, CreateSuggestionRequest request)
    {
        var response = await _client.AuthenticatedPostAsync(
            $"/api/v1/trips/{tripId}/suggestions", accessToken, request, Guid.NewGuid().ToString());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SuggestionResponse>(JsonOptions))!;
    }

    private async Task<TripResponse> CreateTripAsync(string accessToken)
    {
        var request = new CreateTripRequest(
            "Lake Trip", null, "Lake Tahoe", "123 Shore Rd",
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5), "America/Los_Angeles", 48);

        var response = await _client.AuthenticatedPostAsync("/api/v1/trips", accessToken, request, Guid.NewGuid().ToString());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TripResponse>(JsonOptions))!;
    }
}
