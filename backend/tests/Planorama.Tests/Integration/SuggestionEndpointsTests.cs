using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Planorama.Api.Contracts.Auth;
using Planorama.Api.Contracts.Me;
using Planorama.Api.Contracts.Suggestions;
using Planorama.Api.Contracts.Trips;
using Planorama.Core.Domain;
using Xunit;

namespace Planorama.Tests.Integration;

[Collection("Api")]
public class SuggestionEndpointsTests(PlanoramaWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Member_can_create_a_custom_suggestion()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);

        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, login.Tokens.AccessToken, new CreateSuggestionRequest(null, "Mini golf", "Rainy day backup", "5 Pier Ave", null, null, 90, null));

        Assert.Equal("Mini golf", suggestion.Title);
        Assert.Equal(SuggestionSource.Custom, suggestion.Source);
        Assert.Equal(SuggestionStatus.Voting, suggestion.Status);
        Assert.Null(suggestion.Resolution);
        Assert.Equal(FakeGeocodingProvider.Latitude, suggestion.Lat);
    }

    [Fact]
    public async Task Creating_from_a_place_takes_its_details_from_the_provider_not_the_client()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);

        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, login.Tokens.AccessToken, new CreateSuggestionRequest("place-1", null, null, null, null, null, null, null));

        Assert.Equal(SuggestionSource.Geoapify, suggestion.Source);
        Assert.Equal("Harbour Museum", suggestion.Title);
        Assert.Equal("1 Dock Rd", suggestion.Address);
        Assert.Equal(51.5074, suggestion.Lat);
    }

    [Fact]
    public async Task Creating_a_suggestion_without_a_title_or_place_returns_400()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);

        var response = await _client.AuthenticatedPostAsync(
            $"/api/v1/trips/{trip.Id}/suggestions", login.Tokens.AccessToken,
            new CreateSuggestionRequest(null, null, null, null, null, null, null, null), Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Non_members_cannot_create_list_or_read_suggestions()
    {
        var owner = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var outsider = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(owner.Tokens.AccessToken);
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Mini golf", null, null, null, null, null, null));

        var created = await _client.AuthenticatedPostAsync(
            $"/api/v1/trips/{trip.Id}/suggestions", outsider.Tokens.AccessToken,
            new CreateSuggestionRequest(null, "Sneaky", null, null, null, null, null, null), Guid.NewGuid().ToString());
        var listed = await _client.AuthenticatedGetAsync($"/api/v1/trips/{trip.Id}/suggestions", outsider.Tokens.AccessToken);
        var read = await _client.AuthenticatedGetAsync($"/api/v1/suggestions/{suggestion.Id}", outsider.Tokens.AccessToken);
        var voted = await _client.AuthenticatedPutAsync(
            $"/api/v1/suggestions/{suggestion.Id}/vote", outsider.Tokens.AccessToken, new CastVoteRequest(VoteValue.Yes));

        Assert.Equal(HttpStatusCode.NotFound, created.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, listed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, voted.StatusCode);
    }

    [Fact]
    public async Task Suggestion_routes_require_authentication()
    {
        var anonymous = await _client.GetAsync($"/api/v1/suggestions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Tallies_and_votes_are_withheld_until_the_caller_votes()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Mini golf", null, null, null, null, null, null));

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);

        // The second member hasn't voted, so the API must not reveal which way the room is leaning.
        SuggestionResponse asNonVoter = await GetSuggestionAsync(suggestion.Id, member.Tokens.AccessToken);
        Assert.False(asNonVoter.HasVoted);
        Assert.Null(asNonVoter.YourVote);
        Assert.Null(asNonVoter.YesCount);
        Assert.Null(asNonVoter.NoCount);
        Assert.Null(asNonVoter.Votes);

        // Casting a vote unlocks the tally.
        SuggestionResponse afterVoting = await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.No);
        Assert.True(afterVoting.HasVoted);
        Assert.Equal(VoteValue.No, afterVoting.YourVote);
        Assert.Equal(1, afterVoting.YesCount);
        Assert.Equal(1, afterVoting.NoCount);
        Assert.Equal(2, afterVoting.Votes!.Count);
    }

    [Fact]
    public async Task The_list_endpoint_withholds_tallies_too()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Mini golf", null, null, null, null, null, null));
        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);

        var response = await _client.AuthenticatedGetAsync($"/api/v1/trips/{trip.Id}/suggestions", member.Tokens.AccessToken);
        var listed = await response.Content.ReadFromJsonAsync<List<SuggestionResponse>>(JsonOptions);

        SuggestionResponse only = Assert.Single(listed!);
        Assert.Null(only.YesCount);
        Assert.Null(only.Votes);
    }

    [Fact]
    public async Task A_suggesters_lone_vote_does_not_count_toward_the_tally()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, login.Tokens.AccessToken, new CreateSuggestionRequest(null, "Mini golf", null, null, null, null, null, null));

        SuggestionResponse afterSelfVote = await VoteAsync(suggestion.Id, login.Tokens.AccessToken, VoteValue.Yes);

        // Attributed vote is visible, but it carries no weight until someone else votes (spec §6.4).
        Assert.Single(afterSelfVote.Votes!);
        Assert.Equal(0, afterSelfVote.YesCount);
        Assert.Equal(0, afterSelfVote.NoCount);
    }

    [Fact]
    public async Task A_member_can_change_their_vote_without_creating_a_second_one()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Mini golf", null, null, null, null, null, null));

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);
        await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.Yes);
        SuggestionResponse changed = await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.No);

        Assert.Equal(2, changed.Votes!.Count);
        Assert.Equal(1, changed.YesCount);
        Assert.Equal(1, changed.NoCount);
    }

    [Fact]
    public async Task Voting_window_is_clamped_and_never_taken_raw_from_the_client()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        TripResponse trip = await CreateTripAsync(login.Tokens.AccessToken);

        var absurdDeadline = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, login.Tokens.AccessToken,
            new CreateSuggestionRequest(null, "Mini golf", null, null, null, null, null, absurdDeadline));

        // The fixture trip's dates are already in the past, so §6.1's clamp lands behind `now` and
        // the 1h floor takes over — either way the client's requested deadline is discarded.
        Assert.NotEqual(absurdDeadline, suggestion.VotingClosesAt);
        Assert.InRange(
            suggestion.VotingClosesAt,
            DateTimeOffset.UtcNow.AddMinutes(55),
            DateTimeOffset.UtcNow.AddMinutes(65));
    }

    [Fact]
    public async Task Creating_a_suggestion_emails_the_other_accepted_member_but_not_the_suggester()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        var jobs = (NoOpBackgroundJobClient)factory.Services.GetRequiredService<Hangfire.IBackgroundJobClient>();

        await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Notify test golf", null, null, null, null, null, null));

        var notified = jobs.EnqueuedJobs
            .Where(j => j.Method.Name == nameof(Planorama.Core.Jobs.IEmailDispatchJob.SendSuggestionAddedAsync)
                && (string)j.Args[3]! == "Notify test golf")
            .ToList();

        var notifiedEmail = Assert.Single(notified);
        Assert.Equal(member.User.Email, notifiedEmail.Args[0]);
    }

    [Fact]
    public async Task Creating_a_suggestion_does_not_email_a_member_who_opted_out()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        var jobs = (NoOpBackgroundJobClient)factory.Services.GetRequiredService<Hangfire.IBackgroundJobClient>();

        var optOut = await _client.AuthenticatedPatchAsync(
            "/api/v1/me/settings", member.Tokens.AccessToken, new UpdateSettingsRequest(ReminderOffset.TwelveHours, false, false));
        optOut.EnsureSuccessStatusCode();

        await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, new CreateSuggestionRequest(null, "Opted out golf", null, null, null, null, null, null));

        var notified = jobs.EnqueuedJobs
            .Where(j => j.Method.Name == nameof(Planorama.Core.Jobs.IEmailDispatchJob.SendSuggestionAddedAsync)
                && (string)j.Args[3]! == "Opted out golf");

        Assert.Empty(notified);
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

    private async Task<SuggestionResponse> GetSuggestionAsync(Guid suggestionId, string accessToken)
    {
        var response = await _client.AuthenticatedGetAsync($"/api/v1/suggestions/{suggestionId}", accessToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SuggestionResponse>(JsonOptions))!;
    }

    private async Task<SuggestionResponse> VoteAsync(Guid suggestionId, string accessToken, VoteValue value)
    {
        var response = await _client.AuthenticatedPutAsync(
            $"/api/v1/suggestions/{suggestionId}/vote", accessToken, new CastVoteRequest(value));
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
