using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planorama.Api.Contracts.Auth;
using Planorama.Api.Contracts.Suggestions;
using Planorama.Api.Contracts.Trips;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Jobs;
using Planorama.Core.Suggestions;
using Xunit;

namespace Planorama.Tests.Integration;

[Collection("Api")]
public class VotingResolutionTests(PlanoramaWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Majority_yes_approves_and_resolves_before_the_window_closes()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Majority yes golf");

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);
        await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.Yes);

        // Both accepted members have voted, window is still ~48h out — early close (spec §6.6) is
        // the only thing that can resolve this.
        await ResolveAsync();

        SuggestionResponse resolved = await GetSuggestionAsync(suggestion.Id, owner.Tokens.AccessToken);
        Assert.Equal(SuggestionStatus.Approved, resolved.Status);
        Assert.Equal(SuggestionResolution.Majority, resolved.Resolution);
    }

    [Fact]
    public async Task Majority_no_discards_the_suggestion()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Majority no golf");

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.No);
        await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.No);
        await ResolveAsync();

        SuggestionResponse resolved = await GetSuggestionAsync(suggestion.Id, owner.Tokens.AccessToken);
        Assert.Equal(SuggestionStatus.Discarded, resolved.Status);
        Assert.Equal(SuggestionResolution.Majority, resolved.Resolution);
    }

    [Fact]
    public async Task A_tie_is_resolved_by_the_coin_flip()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Tied golf approved");

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);
        await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.No);

        var coinFlip = (FakeCoinFlip)factory.Services.GetRequiredService<ICoinFlip>();
        coinFlip.NextResult = true;
        await ResolveAsync();

        SuggestionResponse resolved = await GetSuggestionAsync(suggestion.Id, owner.Tokens.AccessToken);
        Assert.Equal(SuggestionStatus.Approved, resolved.Status);
        Assert.Equal(SuggestionResolution.CoinFlip, resolved.Resolution);
    }

    [Fact]
    public async Task An_unresolved_tie_can_also_flip_to_discarded()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Tied golf discarded");

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);
        await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.No);

        var coinFlip = (FakeCoinFlip)factory.Services.GetRequiredService<ICoinFlip>();
        coinFlip.NextResult = false;
        await ResolveAsync();

        SuggestionResponse resolved = await GetSuggestionAsync(suggestion.Id, owner.Tokens.AccessToken);
        Assert.Equal(SuggestionStatus.Discarded, resolved.Status);
        Assert.Equal(SuggestionResolution.CoinFlip, resolved.Resolution);
    }

    [Fact]
    public async Task Not_everyone_has_voted_and_the_window_is_still_open_so_nothing_resolves()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Unresolved golf");

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);
        await ResolveAsync();

        SuggestionResponse stillVoting = await GetSuggestionAsync(suggestion.Id, owner.Tokens.AccessToken);
        Assert.Equal(SuggestionStatus.Voting, stillVoting.Status);
        Assert.Null(stillVoting.Resolution);
    }

    [Fact]
    public async Task A_closed_window_resolves_even_without_a_full_vote()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Expired golf");

        // The suggester's lone vote doesn't count (total votes == 1), so this closes as a 0-0 tie.
        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);
        await CloseVotingWindowAsync(suggestion.Id);

        var coinFlip = (FakeCoinFlip)factory.Services.GetRequiredService<ICoinFlip>();
        coinFlip.NextResult = true;
        await ResolveAsync();

        SuggestionResponse resolved = await GetSuggestionAsync(suggestion.Id, owner.Tokens.AccessToken);
        Assert.Equal(SuggestionStatus.Approved, resolved.Status);
        Assert.Equal(SuggestionResolution.CoinFlip, resolved.Resolution);
    }

    [Fact]
    public async Task A_departed_members_vote_is_excluded_at_resolution()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Departed voter golf");

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);
        await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.No);

        // Member leaves before the window closes — with only the owner left as an accepted member,
        // this becomes an unopposed self-vote (total == 1), which doesn't count (spec §6.4).
        await DepartMemberAsync(trip.Id, member.User.Id);
        await CloseVotingWindowAsync(suggestion.Id);
        await ResolveAsync();

        SuggestionResponse resolved = await GetSuggestionAsync(suggestion.Id, owner.Tokens.AccessToken);
        Assert.Equal(SuggestionResolution.CoinFlip, resolved.Resolution);
    }

    [Fact]
    public async Task Resolution_emails_every_accepted_member_who_opted_in()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Notify result golf");

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);
        await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.Yes);
        await ResolveAsync();

        var jobs = (NoOpBackgroundJobClient)factory.Services.GetRequiredService<IBackgroundJobClient>();
        var notified = jobs.EnqueuedJobs
            .Where(j => j.Method.Name == nameof(IEmailDispatchJob.SendVoteResultAsync)
                && (string)j.Args[3]! == "Notify result golf")
            .Select(j => (string)j.Args[0]!)
            .ToList();

        Assert.Contains(owner.User.Email, notified);
        Assert.Contains(member.User.Email, notified);
    }

    [Fact]
    public async Task Creator_can_force_approve_before_resolution()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Override force approve golf");

        // Nobody has voted — an override still works at any time (spec §6.7).
        SuggestionResponse overridden = await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);

        Assert.Equal(SuggestionStatus.Approved, overridden.Status);
        Assert.Equal(SuggestionResolution.Manual, overridden.Resolution);
        Assert.NotNull(overridden.ResolvedAt);
    }

    [Fact]
    public async Task Creator_can_veto_an_already_resolved_suggestion()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Override veto golf");

        await VoteAsync(suggestion.Id, owner.Tokens.AccessToken, VoteValue.Yes);
        await VoteAsync(suggestion.Id, member.Tokens.AccessToken, VoteValue.Yes);
        await ResolveAsync();

        SuggestionResponse alreadyApproved = await GetSuggestionAsync(suggestion.Id, owner.Tokens.AccessToken);
        Assert.Equal(SuggestionStatus.Approved, alreadyApproved.Status);

        // The creator overrides the machine-resolved outcome — always allowed (spec §6.7).
        SuggestionResponse vetoed = await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: false);

        Assert.Equal(SuggestionStatus.Discarded, vetoed.Status);
        Assert.Equal(SuggestionResolution.Manual, vetoed.Resolution);
    }

    [Fact]
    public async Task A_non_creator_member_cannot_override()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Override forbidden golf");

        var response = await _client.AuthenticatedPutAsync(
            $"/api/v1/suggestions/{suggestion.Id}/override", member.Tokens.AccessToken, new { approved = true });

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_override_notifies_every_accepted_member_who_opted_in()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(trip.Id, owner.Tokens.AccessToken, "Override notify golf");

        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);

        var jobs = (NoOpBackgroundJobClient)factory.Services.GetRequiredService<IBackgroundJobClient>();
        var notified = jobs.EnqueuedJobs
            .Where(j => j.Method.Name == nameof(IEmailDispatchJob.SendVoteResultAsync)
                && (string)j.Args[3]! == "Override notify golf")
            .Select(j => (string)j.Args[0]!)
            .ToList();

        Assert.Contains(owner.User.Email, notified);
        Assert.Contains(member.User.Email, notified);
    }

    private async Task<SuggestionResponse> OverrideAsync(Guid suggestionId, string accessToken, bool approved)
    {
        var response = await _client.AuthenticatedPutAsync(
            $"/api/v1/suggestions/{suggestionId}/override", accessToken, new OverrideSuggestionRequest(approved));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SuggestionResponse>(JsonOptions))!;
    }

    private async Task ResolveAsync()
    {
        using var scope = factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IVotingResolutionJob>();
        await job.ResolveDueSuggestionsAsync();
    }

    private async Task CloseVotingWindowAsync(Guid suggestionId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlanoramaDbContext>();
        var suggestion = await db.Suggestions.FirstAsync(s => s.Id == suggestionId);
        suggestion.VotingClosesAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
    }

    private async Task DepartMemberAsync(Guid tripId, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlanoramaDbContext>();
        var member = await db.TripMembers.FirstAsync(m => m.TripId == tripId && m.UserId == userId);
        member.Status = TripMemberStatus.Declined;
        await db.SaveChangesAsync();
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

    private async Task<SuggestionResponse> CreateSuggestionAsync(Guid tripId, string accessToken, string title)
    {
        var response = await _client.AuthenticatedPostAsync(
            $"/api/v1/trips/{tripId}/suggestions", accessToken,
            new CreateSuggestionRequest(null, title, null, null, null, null, null, null), Guid.NewGuid().ToString());
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
