using Planorama.Core.Domain;

namespace Planorama.Core.Suggestions;

/// <param name="HasVoted">Whether the requesting member has cast their own vote.</param>
/// <param name="YourVote">The requesting member's own vote, always visible to them.</param>
/// <param name="YesCount">Eligible yes votes — <c>null</c> while withheld (see spec §6.3).</param>
/// <param name="NoCount">Eligible no votes — <c>null</c> while withheld.</param>
/// <param name="Votes">Attributed votes — <c>null</c> while withheld.</param>
public record SuggestionResult(
    Guid Id,
    Guid TripId,
    Guid SuggestedById,
    string SuggestedByName,
    SuggestionSource Source,
    string? ProviderPlaceId,
    string Title,
    string? Description,
    double? PlaceLat,
    double? PlaceLng,
    string? Address,
    decimal? ExternalRating,
    DateOnly? ProposedDate,
    TimeOnly? ProposedStartTime,
    int? DurationMinutes,
    DateTimeOffset VotingClosesAt,
    SuggestionStatus Status,
    SuggestionResolution? Resolution,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset CreatedAt,
    bool HasVoted,
    VoteValue? YourVote,
    int? YesCount,
    int? NoCount,
    IReadOnlyList<AttributedVote>? Votes);

public record AttributedVote(Guid UserId, string DisplayName, VoteValue Value, DateTimeOffset CastAt);
