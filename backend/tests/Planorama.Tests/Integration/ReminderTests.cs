using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planorama.Api.Contracts.Auth;
using Planorama.Api.Contracts.Itinerary;
using Planorama.Api.Contracts.Me;
using Planorama.Api.Contracts.Suggestions;
using Planorama.Api.Contracts.Trips;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Xunit;

namespace Planorama.Tests.Integration;

[Collection("Api")]
public class ReminderTests(PlanoramaWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Approving_a_scheduled_suggestion_schedules_a_reminder_per_accepted_member()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, "Reminder kayaking", new DateOnly(2030, 6, 1), new TimeOnly(9, 0), 60);

        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);
        ItineraryItemResponse item = Assert.Single(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));

        List<Reminder> reminders = await GetRemindersAsync(item.Id);
        Assert.Equal(2, reminders.Count);
        Assert.Contains(reminders, r => r.UserId == owner.User.Id);
        Assert.Contains(reminders, r => r.UserId == member.User.Id);
    }

    [Fact]
    public async Task Reminder_is_timed_at_the_members_own_offset_before_start()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();

        var patch = await _client.AuthenticatedPatchAsync(
            "/api/v1/me/settings", owner.Tokens.AccessToken, new UpdateSettingsRequest(ReminderOffset.OneHour, true, false));
        patch.EnsureSuccessStatusCode();

        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, "Offset kayaking", new DateOnly(2030, 6, 1), new TimeOnly(9, 0), 60);
        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);
        ItineraryItemResponse item = Assert.Single(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));

        List<Reminder> reminders = await GetRemindersAsync(item.Id);
        Reminder ownerReminder = reminders.Single(r => r.UserId == owner.User.Id);

        // America/Los_Angeles is UTC-7 in June (PDT) — 09:00 local = 16:00 UTC, minus the 1h offset.
        Assert.Equal(new DateTimeOffset(2030, 6, 1, 15, 0, 0, TimeSpan.Zero), ownerReminder.ScheduledForUtc);
    }

    [Fact]
    public async Task A_member_who_opted_out_of_email_gets_no_reminder()
    {
        (var owner, var member, TripResponse trip) = await CreateTripWithTwoMembersAsync();

        var patch = await _client.AuthenticatedPatchAsync(
            "/api/v1/me/settings", member.Tokens.AccessToken, new UpdateSettingsRequest(ReminderOffset.TwelveHours, false, false));
        patch.EnsureSuccessStatusCode();

        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, "Opted out kayaking", new DateOnly(2030, 6, 1), new TimeOnly(9, 0), 60);
        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);
        ItineraryItemResponse item = Assert.Single(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));

        List<Reminder> reminders = await GetRemindersAsync(item.Id);
        Assert.DoesNotContain(reminders, r => r.UserId == member.User.Id);
    }

    [Fact]
    public async Task An_unscheduled_approval_schedules_no_reminder()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, "Unscheduled reminder golf", null, null, null);

        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);
        ItineraryItemResponse item = Assert.Single(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));

        Assert.Empty(await GetRemindersAsync(item.Id));
    }

    [Fact]
    public async Task Vetoing_a_scheduled_approval_cancels_its_reminders()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, "Vetoed reminder golf", new DateOnly(2030, 6, 1), new TimeOnly(9, 0), 60);
        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);
        ItineraryItemResponse item = Assert.Single(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));
        Assert.NotEmpty(await GetRemindersAsync(item.Id));

        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: false);

        Assert.Empty(await GetRemindersAsync(item.Id));
    }

    [Fact]
    public async Task Rescheduling_an_item_replaces_its_reminders()
    {
        (var owner, _, TripResponse trip) = await CreateTripWithTwoMembersAsync();
        SuggestionResponse suggestion = await CreateSuggestionAsync(
            trip.Id, owner.Tokens.AccessToken, "Rescheduled reminder golf", new DateOnly(2030, 6, 1), new TimeOnly(9, 0), 60);
        await OverrideAsync(suggestion.Id, owner.Tokens.AccessToken, approved: true);
        ItineraryItemResponse item = Assert.Single(await GetItineraryAsync(trip.Id, owner.Tokens.AccessToken));
        List<Reminder> before = await GetRemindersAsync(item.Id);

        var request = new UpdateItineraryItemRequest(new DateOnly(2030, 7, 1), new TimeOnly(10, 0), new TimeOnly(11, 0), 0, null);
        var response = await _client.AuthenticatedPatchAsync($"/api/v1/itinerary-items/{item.Id}", owner.Tokens.AccessToken, request);
        response.EnsureSuccessStatusCode();

        List<Reminder> after = await GetRemindersAsync(item.Id);
        Assert.Equal(before.Count, after.Count);
        Assert.Empty(before.Select(r => r.HangfireJobId).Intersect(after.Select(r => r.HangfireJobId)));
        Assert.All(after, r => Assert.NotEqual(before[0].ScheduledForUtc, r.ScheduledForUtc));
    }

    private async Task<List<Reminder>> GetRemindersAsync(Guid itineraryItemId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlanoramaDbContext>();
        return await db.Reminders.Where(r => r.ItineraryItemId == itineraryItemId).ToListAsync();
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

    private async Task<SuggestionResponse> CreateSuggestionAsync(
        Guid tripId, string accessToken, string title, DateOnly? proposedDate, TimeOnly? proposedStartTime, int? durationMinutes)
    {
        var response = await _client.AuthenticatedPostAsync(
            $"/api/v1/trips/{tripId}/suggestions", accessToken,
            new CreateSuggestionRequest(null, title, null, null, proposedDate, proposedStartTime, durationMinutes, null), Guid.NewGuid().ToString());
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
