using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Itinerary;
using Planorama.Core.Jobs;
using Planorama.Core.Notifications;

namespace Planorama.Core.Suggestions;

/// <inheritdoc cref="IVotingResolutionJob"/>
public class VotingResolutionService(
    PlanoramaDbContext db,
    ICoinFlip coinFlip,
    VoteResultNotifier notifier,
    ItinerarySyncService itinerarySync,
    TimeProvider timeProvider,
    ILogger<VotingResolutionService> logger) : IVotingResolutionJob
{
    /// <inheritdoc/>
    public async Task ResolveDueSuggestionsAsync()
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<Suggestion> voting = await db.Suggestions.Where(s => s.Status == SuggestionStatus.Voting).ToListAsync();

        foreach (Suggestion suggestion in voting)
        {
            List<Guid> acceptedMemberIds = await db.TripMembers
                .Where(m => m.TripId == suggestion.TripId && m.Status == TripMemberStatus.Accepted)
                .Select(m => m.UserId)
                .ToListAsync();

            // Departed members' votes are excluded here rather than at cast time (spec §6 edge cases):
            // a member could leave after voting, and their vote must stop counting immediately.
            List<CountedVote> votes = await db.Votes
                .Where(v => v.SuggestionId == suggestion.Id && acceptedMemberIds.Contains(v.UserId))
                .Select(v => new CountedVote(v.UserId, v.Value))
                .ToListAsync();

            bool windowClosed = suggestion.VotingClosesAt <= now;
            bool allVoted = acceptedMemberIds.Count > 0 && votes.Count >= acceptedMemberIds.Count;
            if (!windowClosed && !allVoted)
            {
                continue;
            }

            await ResolveAsync(suggestion, votes, now);
        }
    }

    private async Task ResolveAsync(Suggestion suggestion, List<CountedVote> votes, DateTimeOffset now)
    {
        (int yes, int no) = VoteTally.Count(votes, suggestion.SuggestedById);

        if (yes > no)
        {
            suggestion.Status = SuggestionStatus.Approved;
            suggestion.Resolution = SuggestionResolution.Majority;
        }
        else if (no > yes)
        {
            suggestion.Status = SuggestionStatus.Discarded;
            suggestion.Resolution = SuggestionResolution.Majority;
        }
        else
        {
            // Tie, including 0-0 after rule 4 filtering (spec §6.5) — server-side crypto-random
            // coin flip, logged here as the transparency/audit trail (no separate audit table yet).
            bool approved = coinFlip.FlipApproved();
            suggestion.Status = approved ? SuggestionStatus.Approved : SuggestionStatus.Discarded;
            suggestion.Resolution = SuggestionResolution.CoinFlip;
            logger.LogInformation(
                "Suggestion {SuggestionId} tied {Yes}-{No}; coin flip resolved to {Status}",
                suggestion.Id, yes, no, suggestion.Status);
        }

        suggestion.ResolvedAt = now;
        await db.SaveChangesAsync();

        await itinerarySync.SyncAsync(suggestion, CancellationToken.None);
        await notifier.NotifyAsync(suggestion.TripId, suggestion.Title, suggestion.Status == SuggestionStatus.Approved, CancellationToken.None);
    }
}
