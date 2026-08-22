namespace Planorama.Core.Suggestions;

/// <summary>Abstracts the tie-break coin flip (spec §6.5) behind an interface so resolution
/// outcomes are deterministic in tests, the same reason places/routing/geocoding sit behind
/// interfaces.</summary>
public interface ICoinFlip
{
    /// <returns>True if the flip favors approving the tied suggestion; false if it favors discarding it.</returns>
    bool FlipApproved();
}
