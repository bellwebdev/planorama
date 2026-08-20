using System.Security.Claims;
using Planorama.Api.Auth;
using Planorama.Api.Contracts.Places;
using Planorama.Api.Filters;
using Planorama.Core.Integrations;
using Planorama.Core.Places;

namespace Planorama.Api.Endpoints;

public static class PlaceEndpoints
{
    /// <summary>Maps the proxied places and routing routes. Third-party calls happen behind these
    /// endpoints only — provider keys never reach a client. Trip-scoped routes enforce membership
    /// inside <see cref="IPlaceService"/>.</summary>
    public static void MapPlaceEndpoints(this RouteGroupBuilder v1)
    {
        var places = v1.MapGroup("/places").WithTags("Places").RequireAuthorization();

        places.MapGet("/categories", () => Results.Ok(PlaceCategoryResponse.All));

        places.MapGet("/{providerId}", async (string providerId, IPlaceService placeService, CancellationToken ct) =>
            Results.Ok(PlaceDetailResponse.FromResult(await placeService.GetDetailAsync(providerId, ct))));

        var trips = v1.MapGroup("/trips").WithTags("Places").RequireAuthorization();

        trips.MapGet("/{id:guid}/places/search", async (
                Guid id, [AsParameters] PlaceSearchRequest request, ClaimsPrincipal user, IPlaceService placeService, CancellationToken ct) =>
            {
                IReadOnlyList<PlaceResult> results = await placeService.SearchNearStayAsync(
                    id, user.GetUserId(), request.Category!.Value, request.EffectiveRadius, request.Q, request.EffectiveLimit, ct);
                return Results.Ok(results.Select(PlaceResponse.FromResult));
            })
            .AddEndpointFilter<ValidationFilter<PlaceSearchRequest>>();

        trips.MapGet("/{id:guid}/route", async (
                Guid id, [AsParameters] RouteRequest request, ClaimsPrincipal user, IPlaceService placeService, CancellationToken ct) =>
            {
                RouteResult route = await placeService.GetRouteFromStayAsync(
                    id, user.GetUserId(), new GeoPoint(request.ToLat!.Value, request.ToLng!.Value), request.EffectiveMode, ct);
                return Results.Ok(RouteResponse.FromResult(route, request.EffectiveMode));
            })
            .AddEndpointFilter<ValidationFilter<RouteRequest>>();
    }
}
