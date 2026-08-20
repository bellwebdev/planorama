namespace Planorama.Api.Contracts.Suggestions;

/// <param name="ProviderPlaceId">Set when suggesting a place from search; its details are re-fetched server-side.</param>
/// <param name="Title">Required for a custom suggestion; optional override of a provider place's name.</param>
/// <param name="VotingClosesAt">Optional requested deadline — always clamped server-side per spec §6.1.</param>
public record CreateSuggestionRequest(
    string? ProviderPlaceId,
    string? Title,
    string? Description,
    string? Address,
    DateOnly? ProposedDate,
    TimeOnly? ProposedStartTime,
    int? DurationMinutes,
    DateTimeOffset? VotingClosesAt);
