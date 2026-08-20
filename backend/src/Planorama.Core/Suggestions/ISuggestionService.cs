using Planorama.Core.Domain;

namespace Planorama.Core.Suggestions;

public interface ISuggestionService
{
    /// <summary>Creates a suggestion, either from a provider place or as a custom entry.</summary>
    /// <exception cref="Exceptions.TripNotFoundException">The trip doesn't exist, or the caller isn't an accepted member.</exception>
    /// <exception cref="Exceptions.SuggestionPlaceNotResolvedException">Neither a usable provider place nor a title was supplied.</exception>
    Task<SuggestionResult> CreateAsync(Guid tripId, Guid userId, CreateSuggestionCommand command, CancellationToken ct);

    /// <exception cref="Exceptions.TripNotFoundException">The trip doesn't exist, or the caller isn't an accepted member.</exception>
    Task<IReadOnlyList<SuggestionResult>> ListForTripAsync(Guid tripId, Guid userId, CancellationToken ct);

    /// <exception cref="Exceptions.SuggestionNotFoundException">No such suggestion, or the caller isn't an accepted member of its trip.</exception>
    Task<SuggestionResult> GetByIdAsync(Guid suggestionId, Guid userId, CancellationToken ct);

    /// <summary>Casts or changes the caller's vote. Idempotent per member — one row, updated.</summary>
    /// <exception cref="Exceptions.SuggestionNotFoundException">No such suggestion, or the caller isn't an accepted member of its trip.</exception>
    /// <exception cref="Exceptions.VotingClosedException">The voting window has already closed.</exception>
    Task<SuggestionResult> CastVoteAsync(Guid suggestionId, Guid userId, VoteValue value, CancellationToken ct);
}

/// <param name="ProviderPlaceId">When set, place data is re-fetched from the provider rather than trusted from the client.</param>
/// <param name="Title">Required for a custom suggestion; optional override of the provider's place name.</param>
/// <param name="VotingClosesAt">Suggester's requested deadline; clamped server-side per spec §6.1.</param>
public record CreateSuggestionCommand(
    string? ProviderPlaceId,
    string? Title,
    string? Description,
    string? Address,
    DateOnly? ProposedDate,
    TimeOnly? ProposedStartTime,
    int? DurationMinutes,
    DateTimeOffset? VotingClosesAt);
