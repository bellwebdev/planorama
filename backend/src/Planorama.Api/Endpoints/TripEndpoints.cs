using System.Security.Claims;
using Planorama.Api.Auth;
using Planorama.Api.Contracts.Trips;
using Planorama.Api.Filters;
using Planorama.Core.Trips;

namespace Planorama.Api.Endpoints;

public static class TripEndpoints
{
    /// <summary>Maps the <c>/trips</c> routes. All authenticated; per-trip routes additionally
    /// enforce membership (and creator-only where noted) inside <see cref="ITripService"/>.</summary>
    public static void MapTripEndpoints(this RouteGroupBuilder v1)
    {
        var trips = v1.MapGroup("/trips").WithTags("Trips").RequireAuthorization();

        trips.MapPost("/", async (CreateTripRequest request, ClaimsPrincipal user, ITripService tripService, CancellationToken ct) =>
            {
                var result = await tripService.CreateAsync(
                    user.GetUserId(), request.Name, request.Description, request.LocationName, request.StayAddress,
                    request.StartDate, request.EndDate, request.Timezone, request.DefaultVotingWindowHours ?? 48, ct);
                return Results.Created($"/api/v1/trips/{result.Id}", TripResponse.FromResult(result));
            })
            .AddEndpointFilter<ValidationFilter<CreateTripRequest>>()
            .AddEndpointFilter(new IdempotencyFilter("trips"));

        trips.MapGet("/", async (ClaimsPrincipal user, ITripService tripService, CancellationToken ct) =>
            Results.Ok((await tripService.ListForUserAsync(user.GetUserId(), ct)).Select(TripResponse.FromResult)));

        trips.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, ITripService tripService, CancellationToken ct) =>
            Results.Ok(TripResponse.FromResult(await tripService.GetByIdAsync(id, user.GetUserId(), ct))));

        trips.MapPatch("/{id:guid}", async (Guid id, UpdateTripRequest request, ClaimsPrincipal user, ITripService tripService, CancellationToken ct) =>
                Results.Ok(TripResponse.FromResult(await tripService.UpdateAsync(
                    id, user.GetUserId(), request.Name, request.Description, request.LocationName, request.StayAddress,
                    request.StartDate, request.EndDate, request.Timezone, request.DefaultVotingWindowHours, request.Status, ct))))
            .AddEndpointFilter<ValidationFilter<UpdateTripRequest>>();

        trips.MapPost("/{id:guid}/invites", async (Guid id, CreateInviteRequest request, ClaimsPrincipal user, IInviteService inviteService, CancellationToken ct) =>
            {
                var result = await inviteService.CreateInviteAsync(id, user.GetUserId(), request.Via, request.Contact, ct);
                return Results.Ok(new InviteResponse(result.Token, result.InvitedVia, result.Contact, result.ExpiresAt));
            })
            .AddEndpointFilter<ValidationFilter<CreateInviteRequest>>()
            .AddEndpointFilter(new IdempotencyFilter("trips/invites"));
    }
}
