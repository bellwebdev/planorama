using System.Security.Claims;
using Planorama.Api.Auth;
using Planorama.Api.Contracts.Me;
using Planorama.Api.Filters;
using Planorama.Core.Profile;

namespace Planorama.Api.Endpoints;

public static class MeEndpoints
{
    /// <summary>Maps the three <c>/me</c> routes. First authenticated route group in the API — everything here requires a valid JWT bearer token.</summary>
    public static void MapMeEndpoints(this RouteGroupBuilder v1)
    {
        var me = v1.MapGroup("/me").WithTags("Me").RequireAuthorization();

        me.MapGet("/", async (ClaimsPrincipal user, IProfileService profileService, CancellationToken ct) =>
            Results.Ok(MapResponse(await profileService.GetProfileAsync(user.GetUserId(), ct))));

        me.MapPatch("/", async (UpdateProfileRequest request, ClaimsPrincipal user, IProfileService profileService, CancellationToken ct) =>
                Results.Ok(MapResponse(await profileService.UpdateDisplayNameAsync(user.GetUserId(), request.DisplayName, ct))))
            .AddEndpointFilter<ValidationFilter<UpdateProfileRequest>>();

        // No IdempotencyFilter: a retried upload just overwrites the same R2 object again —
        // naturally idempotent-safe, not creation-with-side-effects.
        me.MapPost("/avatar", async (IFormFile file, ClaimsPrincipal user, IProfileService profileService, CancellationToken ct) =>
            {
                await using var stream = file.OpenReadStream();
                var profile = await profileService.UpdateAvatarAsync(user.GetUserId(), stream, file.Length, ct);
                return Results.Ok(MapResponse(profile));
            })
            .DisableAntiforgery();
    }

    private static MeResponse MapResponse(ProfileResult profile) =>
        new(profile.UserId, profile.Email, profile.DisplayName, profile.AvatarUrl, profile.CreatedAt);
}
