using System.Security.Claims;
using Planorama.Api.Auth;
using Planorama.Api.Contracts.Suggestions;
using Planorama.Api.Filters;
using Planorama.Core.Suggestions;

namespace Planorama.Api.Endpoints;

public static class SuggestionEndpoints
{
    /// <summary>Maps the suggestion and voting routes. All authenticated; membership (and the
    /// spec §6.3 vote-visibility rule) is enforced inside <see cref="ISuggestionService"/>.</summary>
    public static void MapSuggestionEndpoints(this RouteGroupBuilder v1)
    {
        var trips = v1.MapGroup("/trips").WithTags("Suggestions").RequireAuthorization();

        trips.MapPost("/{id:guid}/suggestions", async (
                Guid id, CreateSuggestionRequest request, ClaimsPrincipal user, ISuggestionService suggestions, CancellationToken ct) =>
            {
                SuggestionResult result = await suggestions.CreateAsync(
                    id,
                    user.GetUserId(),
                    new CreateSuggestionCommand(
                        request.ProviderPlaceId, request.Title, request.Description, request.Address,
                        request.ProposedDate, request.ProposedStartTime, request.DurationMinutes, request.VotingClosesAt),
                    ct);
                return Results.Created($"/api/v1/suggestions/{result.Id}", SuggestionResponse.FromResult(result));
            })
            .AddEndpointFilter<ValidationFilter<CreateSuggestionRequest>>()
            .AddEndpointFilter(new IdempotencyFilter("trips/suggestions"));

        trips.MapGet("/{id:guid}/suggestions", async (Guid id, ClaimsPrincipal user, ISuggestionService suggestions, CancellationToken ct) =>
            Results.Ok((await suggestions.ListForTripAsync(id, user.GetUserId(), ct)).Select(SuggestionResponse.FromResult)));

        var suggestionRoutes = v1.MapGroup("/suggestions").WithTags("Suggestions").RequireAuthorization();

        suggestionRoutes.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, ISuggestionService suggestions, CancellationToken ct) =>
            Results.Ok(SuggestionResponse.FromResult(await suggestions.GetByIdAsync(id, user.GetUserId(), ct))));

        suggestionRoutes.MapPut("/{id:guid}/vote", async (
                Guid id, CastVoteRequest request, ClaimsPrincipal user, ISuggestionService suggestions, CancellationToken ct) =>
            Results.Ok(SuggestionResponse.FromResult(await suggestions.CastVoteAsync(id, user.GetUserId(), request.Value, ct))));
    }
}
