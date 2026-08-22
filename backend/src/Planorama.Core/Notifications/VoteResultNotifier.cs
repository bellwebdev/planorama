using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Planorama.Core.Data;
using Planorama.Core.Jobs;
using Planorama.Core.Options;

namespace Planorama.Core.Notifications;

/// <summary>Shared by both ways a suggestion resolves — the worker's <c>VotingResolutionService</c>
/// and a creator's manual override — so the vote-result email is sent identically either way.</summary>
public class VoteResultNotifier(PlanoramaDbContext db, IBackgroundJobClient backgroundJobClient, IOptions<EmailOptions> emailOptions)
{
    private readonly EmailOptions _email = emailOptions.Value;

    public async Task NotifyAsync(Guid tripId, string suggestionTitle, bool approved, CancellationToken ct)
    {
        string tripName = await db.Trips.AsNoTracking().Where(t => t.Id == tripId).Select(t => t.Name).FirstAsync(ct);
        List<(string Email, string DisplayName)> recipients = await db.NotifiableMembers(tripId).ToListAsync(ct);

        var tripUrl = $"{_email.SuggestionUrlBase}/{tripId}";
        foreach ((string toEmail, string displayName) in recipients)
        {
            backgroundJobClient.Enqueue<IEmailDispatchJob>(
                j => j.SendVoteResultAsync(toEmail, displayName, tripName, suggestionTitle, approved, tripUrl));
        }
    }
}
