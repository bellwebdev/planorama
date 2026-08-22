using System.Security.Claims;
using Planorama.Api.Auth;
using Planorama.Api.Contracts.Itinerary;
using Planorama.Core.Itinerary;

namespace Planorama.Api.Endpoints;

public static class ItineraryEndpoints
{
    /// <summary>Maps the itinerary routes. All authenticated; membership (and the creator-only
    /// scheduling rule) is enforced inside <see cref="IItineraryService"/>.</summary>
    public static void MapItineraryEndpoints(this RouteGroupBuilder v1)
    {
        var trips = v1.MapGroup("/trips").WithTags("Itinerary").RequireAuthorization();

        trips.MapGet("/{id:guid}/itinerary", async (Guid id, ClaimsPrincipal user, IItineraryService itinerary, CancellationToken ct) =>
            Results.Ok((await itinerary.ListForTripAsync(id, user.GetUserId(), ct)).Select(ItineraryItemResponse.FromResult)));

        var items = v1.MapGroup("/itinerary-items").WithTags("Itinerary").RequireAuthorization();

        items.MapPatch("/{id:guid}", async (
                Guid id, UpdateItineraryItemRequest request, ClaimsPrincipal user, IItineraryService itinerary, CancellationToken ct) =>
            Results.Ok(ItineraryItemResponse.FromResult(await itinerary.UpdateAsync(
                id, user.GetUserId(), request.Date, request.StartTime, request.EndTime, request.SortOrder, request.Timezone, ct))));
    }
}
