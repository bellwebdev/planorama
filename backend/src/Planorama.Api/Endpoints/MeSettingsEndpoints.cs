using System.Security.Claims;
using Planorama.Api.Auth;
using Planorama.Api.Contracts.Me;
using Planorama.Api.Filters;
using Planorama.Core.Settings;

namespace Planorama.Api.Endpoints;

public static class MeSettingsEndpoints
{
    /// <summary>Maps the two <c>/me/settings</c> routes. Authenticated, like all <c>/me</c> routes.</summary>
    public static void MapMeSettingsEndpoints(this RouteGroupBuilder v1)
    {
        var settings = v1.MapGroup("/me/settings").WithTags("Me").RequireAuthorization();

        settings.MapGet("/", async (ClaimsPrincipal user, ISettingsService settingsService, CancellationToken ct) =>
            Results.Ok(MapResponse(await settingsService.GetSettingsAsync(user.GetUserId(), ct))));

        settings.MapPatch("/", async (UpdateSettingsRequest request, ClaimsPrincipal user, ISettingsService settingsService, CancellationToken ct) =>
                Results.Ok(MapResponse(await settingsService.UpdateSettingsAsync(
                    user.GetUserId(), request.ReminderOffset, request.NotifyEmail, request.NotifyPush, ct))))
            .AddEndpointFilter<ValidationFilter<UpdateSettingsRequest>>();
    }

    private static SettingsResponse MapResponse(SettingsResult settings) =>
        new(settings.ReminderOffset, settings.NotifyEmail, settings.NotifyPush);
}
