using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planorama.Api.Contracts.Trips;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Xunit;

namespace Planorama.Tests.Integration;

[Collection("Api")]
public class TripEndpointsTests(PlanoramaWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    // Mirrors the JsonStringEnumConverter registered in Program.cs's ConfigureHttpJsonOptions —
    // ReadFromJsonAsync uses default options otherwise, which expect enums as raw numbers.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static CreateTripRequest ValidTripRequest(string name = "Lake Trip") => new(
        name, "Annual family trip", "Lake Tahoe", "123 Shore Rd",
        new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5), "America/Los_Angeles", 48);

    [Fact]
    public async Task Create_trip_returns_201_and_creator_can_fetch_it()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var createResponse = await AuthenticatedPostAsync("/api/v1/trips", login.Tokens.AccessToken, ValidTripRequest(), Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TripResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(login.User.Id, created!.CreatorId);
        Assert.Equal(TripStatus.Draft, created.Status);

        var getResponse = await AuthenticatedGetAsync($"/api/v1/trips/{created.Id}", login.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<TripResponse>(JsonOptions);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task Create_trip_missing_idempotency_key_returns_400()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/trips") { Content = JsonContent.Create(ValidTripRequest()) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Tokens.AccessToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_trips_returns_only_trips_the_caller_belongs_to()
    {
        var loginA = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var loginB = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        await AuthenticatedPostAsync("/api/v1/trips", loginA.Tokens.AccessToken, ValidTripRequest("A's Trip"), Guid.NewGuid().ToString());
        await AuthenticatedPostAsync("/api/v1/trips", loginB.Tokens.AccessToken, ValidTripRequest("B's Trip"), Guid.NewGuid().ToString());

        var response = await AuthenticatedGetAsync("/api/v1/trips", loginA.Tokens.AccessToken);
        var trips = await response.Content.ReadFromJsonAsync<List<TripResponse>>(JsonOptions);

        Assert.Contains(trips!, t => t.Name == "A's Trip");
        Assert.DoesNotContain(trips!, t => t.Name == "B's Trip");
    }

    [Fact]
    public async Task Get_trip_as_non_member_returns_404()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var outsider = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var trip = await CreateTripAsync(creator.Tokens.AccessToken);

        var response = await AuthenticatedGetAsync($"/api/v1/trips/{trip.Id}", outsider.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_trip_as_creator_returns_200_with_updated_fields()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);

        var update = new UpdateTripRequest(
            "Renamed Trip", "Updated description", trip.LocationName, trip.StayAddress,
            trip.StartDate, trip.EndDate, trip.Timezone, trip.DefaultVotingWindowHours, TripStatus.Planning);
        var response = await AuthenticatedPatchAsync($"/api/v1/trips/{trip.Id}", creator.Tokens.AccessToken, update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TripResponse>(JsonOptions);
        Assert.Equal("Renamed Trip", updated!.Name);
        Assert.Equal(TripStatus.Planning, updated.Status);
    }

    [Fact]
    public async Task Update_trip_as_non_member_returns_404()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var outsider = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);

        var update = new UpdateTripRequest(
            trip.Name, trip.Description, trip.LocationName, trip.StayAddress,
            trip.StartDate, trip.EndDate, trip.Timezone, trip.DefaultVotingWindowHours, trip.Status);
        var response = await AuthenticatedPatchAsync($"/api/v1/trips/{trip.Id}", outsider.Tokens.AccessToken, update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_trip_as_member_who_is_not_creator_returns_403()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var member = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);
        var invite = await CreateLinkInviteAsync(trip.Id, creator.Tokens.AccessToken);
        await AuthenticatedPostAsync($"/api/v1/invites/{invite.Token}/accept", member.Tokens.AccessToken);

        var update = new UpdateTripRequest(
            trip.Name, trip.Description, trip.LocationName, trip.StayAddress,
            trip.StartDate, trip.EndDate, trip.Timezone, trip.DefaultVotingWindowHours, trip.Status);
        var response = await AuthenticatedPatchAsync($"/api/v1/trips/{trip.Id}", member.Tokens.AccessToken, update);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_link_invite_returns_token()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);

        var invite = await CreateLinkInviteAsync(trip.Id, creator.Tokens.AccessToken);

        Assert.NotEqual(Guid.Empty, invite.Token);
        Assert.Equal(InvitedVia.Link, invite.InvitedVia);
    }

    [Fact]
    public async Task Create_email_invite_enqueues_send_without_error()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);

        var response = await AuthenticatedPostAsync(
            $"/api/v1/trips/{trip.Id}/invites", creator.Tokens.AccessToken,
            new CreateInviteRequest(InvitedVia.Email, "friend@example.com"), Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invite = await response.Content.ReadFromJsonAsync<InviteResponse>(JsonOptions);
        Assert.Equal("friend@example.com", invite!.Contact);
    }

    [Fact]
    public async Task Create_invite_as_non_member_returns_404()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var outsider = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);

        var response = await AuthenticatedPostAsync(
            $"/api/v1/trips/{trip.Id}/invites", outsider.Tokens.AccessToken,
            new CreateInviteRequest(InvitedVia.Link, null), Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_invite_as_member_who_is_not_creator_returns_403()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var member = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);
        var invite = await CreateLinkInviteAsync(trip.Id, creator.Tokens.AccessToken);
        await AuthenticatedPostAsync($"/api/v1/invites/{invite.Token}/accept", member.Tokens.AccessToken);

        var response = await AuthenticatedPostAsync(
            $"/api/v1/trips/{trip.Id}/invites", member.Tokens.AccessToken,
            new CreateInviteRequest(InvitedVia.Link, null), Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Accept_invite_joins_trip_and_grants_access()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var invitee = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);
        var invite = await CreateLinkInviteAsync(trip.Id, creator.Tokens.AccessToken);

        var acceptResponse = await AuthenticatedPostAsync($"/api/v1/invites/{invite.Token}/accept", invitee.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var getResponse = await AuthenticatedGetAsync($"/api/v1/trips/{trip.Id}", invitee.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Accept_invite_twice_is_idempotent()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var invitee = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);
        var invite = await CreateLinkInviteAsync(trip.Id, creator.Tokens.AccessToken);

        var first = await AuthenticatedPostAsync($"/api/v1/invites/{invite.Token}/accept", invitee.Tokens.AccessToken);
        var second = await AuthenticatedPostAsync($"/api/v1/invites/{invite.Token}/accept", invitee.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Accept_unknown_token_returns_404()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var response = await AuthenticatedPostAsync($"/api/v1/invites/{Guid.NewGuid()}/accept", login.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Accept_expired_invite_returns_404()
    {
        var creator = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var invitee = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());
        var trip = await CreateTripAsync(creator.Tokens.AccessToken);
        var expiredToken = await InsertExpiredInvitationAsync(trip.Id);

        var response = await AuthenticatedPostAsync($"/api/v1/invites/{expiredToken}/accept", invitee.Tokens.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<TripResponse> CreateTripAsync(string accessToken)
    {
        var response = await AuthenticatedPostAsync("/api/v1/trips", accessToken, ValidTripRequest(), Guid.NewGuid().ToString());
        return (await response.Content.ReadFromJsonAsync<TripResponse>(JsonOptions))!;
    }

    private async Task<InviteResponse> CreateLinkInviteAsync(Guid tripId, string accessToken)
    {
        var response = await AuthenticatedPostAsync(
            $"/api/v1/trips/{tripId}/invites", accessToken, new CreateInviteRequest(InvitedVia.Link, null), Guid.NewGuid().ToString());
        return (await response.Content.ReadFromJsonAsync<InviteResponse>(JsonOptions))!;
    }

    private async Task<Guid> InsertExpiredInvitationAsync(Guid tripId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlanoramaDbContext>();
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            InvitedVia = InvitedVia.Link,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync();
        return invitation.Token;
    }

    private Task<HttpResponseMessage> AuthenticatedGetAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> AuthenticatedPatchAsync<T>(string url, string accessToken, T body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> AuthenticatedPostAsync<T>(string url, string accessToken, T body, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> AuthenticatedPostAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(request);
    }
}
