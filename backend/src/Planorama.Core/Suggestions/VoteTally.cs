using Planorama.Core.Domain;

namespace Planorama.Core.Suggestions;

/// <summary>Spec §6.4's eligibility rule, as a pure function shared by the tally shown to members
/// and the Phase 2 resolution job — so what a member sees can't disagree with how it resolves.</summary>
public static class VoteTally
{
    /// <summary>Counts the votes that actually decide a suggestion.</summary>
    /// <param name="votes">Votes from currently-accepted members only; callers filter departed members out first.</param>
    /// <param name="suggestedById">The suggester, whose own vote is conditionally excluded.</param>
    /// <returns>The eligible yes/no counts.</returns>
    public static (int Yes, int No) Count(IReadOnlyCollection<CountedVote> votes, Guid suggestedById)
    {
        // The suggester's vote only counts once someone else has voted too — otherwise a member
        // could approve their own suggestion unopposed.
        IEnumerable<CountedVote> eligible = votes.Count > 1
            ? votes
            : votes.Where(v => v.UserId != suggestedById);

        var counted = eligible.ToList();
        return (counted.Count(v => v.Value == VoteValue.Yes), counted.Count(v => v.Value == VoteValue.No));
    }
}

public readonly record struct CountedVote(Guid UserId, VoteValue Value);
