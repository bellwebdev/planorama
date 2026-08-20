namespace Planorama.Core.Domain;

/// <summary>How a suggestion reached its final status. Written by the Phase 2 worker resolution
/// job (and by a creator override); always null while <see cref="SuggestionStatus.Voting"/>.</summary>
public enum SuggestionResolution
{
    Majority,
    CoinFlip,
    NoQuorum,
    Manual,
}
