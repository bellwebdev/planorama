using Planorama.Core.Domain;
using Planorama.Core.Suggestions;

namespace Planorama.Api.Contracts.Suggestions;

/// <param name="YesCount">Null until the caller has voted — see spec §6.3.</param>
/// <param name="NoCount">Null until the caller has voted.</param>
/// <param name="Votes">Null until the caller has voted.</param>
public record SuggestionResponse(
    Guid Id,
    Guid TripId,
    Guid SuggestedById,
    string SuggestedByName,
    SuggestionSource Source,
    string? ProviderPlaceId,
    string Title,
    string? Description,
    double? Lat,
    double? Lng,
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
    IReadOnlyList<VoteResponse>? Votes)
{
    public static SuggestionResponse FromResult(SuggestionResult s) => new(
        s.Id, s.TripId, s.SuggestedById, s.SuggestedByName, s.Source, s.ProviderPlaceId,
        s.Title, s.Description, s.PlaceLat, s.PlaceLng, s.Address, s.ExternalRating,
        s.ProposedDate, s.ProposedStartTime, s.DurationMinutes,
        s.VotingClosesAt, s.Status, s.Resolution, s.ResolvedAt, s.CreatedAt,
        s.HasVoted, s.YourVote, s.YesCount, s.NoCount,
        s.Votes?.Select(v => new VoteResponse(v.UserId, v.DisplayName, v.Value, v.CastAt)).ToList());
}

public record VoteResponse(Guid UserId, string DisplayName, VoteValue Value, DateTimeOffset CastAt);
