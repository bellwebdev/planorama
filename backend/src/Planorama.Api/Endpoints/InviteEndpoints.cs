using System.Security.Claims;
using Planorama.Api.Auth;
using Planorama.Api.Contracts.Trips;
using Planorama.Core.Trips;

namespace Planorama.Api.Endpoints;

public static class InviteEndpoints
{
    /// <summary>Maps <c>POST /invites/{token}/accept</c>. Authenticated — an invitee without an
    /// account registers/logs in first, then the frontend replays the stored token here.</summary>
    public static void MapInviteEndpoints(this RouteGroupBuilder v1)
    {
        var invites = v1.MapGroup("/invites").WithTags("Trips").RequireAuthorization();

        // Naturally idempotent — accepting the same token twice just re-confirms membership, so no
        // IdempotencyFilter (mirrors avatar upload's reasoning in MeEndpoints.cs).
        invites.MapPost("/{token:guid}/accept", async (Guid token, ClaimsPrincipal user, IInviteService inviteService, CancellationToken ct) =>
            Results.Ok(TripResponse.FromResult(await inviteService.AcceptInviteAsync(token, user.GetUserId(), ct))));
    }
}
