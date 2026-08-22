using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Jobs;
using Planorama.Core.Options;

namespace Planorama.Core.Itinerary;

/// <summary>Schedules, reschedules, or cancels each accepted member's event-reminder email for an
/// itinerary item, called whenever the item's schedule changes (spec §8) — placed on the itinerary,
/// rescheduled by the creator, or removed. One reminder per member, timed at their own
/// <see cref="UserSettings.ReminderOffset"/> before the item's start.</summary>
public class ReminderScheduler(
    PlanoramaDbContext db,
    IBackgroundJobClient backgroundJobClient,
    IOptions<EmailOptions> emailOptions,
    ILogger<ReminderScheduler> logger)
{
    private readonly EmailOptions _email = emailOptions.Value;

    /// <summary>Cancels any pending reminders for an item without rescheduling — for an item being
    /// removed entirely (its suggestion was discarded or vetoed).</summary>
    public async Task CancelForItemAsync(Guid itineraryItemId, CancellationToken ct)
    {
        List<Reminder> existing = await db.Reminders.Where(r => r.ItineraryItemId == itineraryItemId).ToListAsync(ct);
        foreach (Reminder reminder in existing)
        {
            backgroundJobClient.Delete(reminder.HangfireJobId);
        }

        db.Reminders.RemoveRange(existing);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Cancels any pending reminders for an item, then schedules fresh ones if it's
    /// currently scheduled — covers both "just placed on the itinerary" and "creator rescheduled it".</summary>
    public async Task RescheduleForItemAsync(Guid itineraryItemId, CancellationToken ct)
    {
        await CancelForItemAsync(itineraryItemId, ct);

        ItineraryItem? item = await db.ItineraryItems
            .AsNoTracking()
            .Include(i => i.Suggestion)
            .FirstOrDefaultAsync(i => i.Id == itineraryItemId, ct);
        if (item is null || item.Date is not { } date || item.StartTime is not { } startTime)
        {
            return; // Deleted, or still in the unscheduled tray — nothing to remind about.
        }

        Trip trip = await db.Trips.AsNoTracking().FirstAsync(t => t.Id == item.TripId, ct);
        TimeZoneInfo timezone = TripTimeZone.Resolve(item.Timezone ?? trip.Timezone, logger);
        DateTimeOffset startUtc = TripTimeZone.ToUtcInstant(date, startTime, timezone);
        string itemTitle = item.Suggestion?.Title ?? trip.Name;
        var tripUrl = $"{_email.SuggestionUrlBase}/{trip.Id}";
        DateTimeOffset now = DateTimeOffset.UtcNow;

        List<(Guid UserId, string Email, string DisplayName, ReminderOffset Offset)> recipients = await db.TripMembers
            .Where(m => m.TripId == item.TripId && m.Status == TripMemberStatus.Accepted)
            .Join(db.Users, m => m.UserId, u => u.Id, (_, u) => u)
            .Where(u => u.Settings == null || u.Settings.NotifyEmail)
            .Select(u => new ValueTuple<Guid, string, string, ReminderOffset>(
                u.Id, u.Email!, u.DisplayName, u.Settings != null ? u.Settings.ReminderOffset : ReminderOffset.TwelveHours))
            .ToListAsync(ct);

        foreach ((Guid userId, string toEmail, string displayName, ReminderOffset offset) in recipients)
        {
            DateTimeOffset sendAt = startUtc - ToTimeSpan(offset);
            if (sendAt <= now)
            {
                continue; // Too close to (or past) the item's start to bother reminding.
            }

            string jobId = backgroundJobClient.Schedule<IEmailDispatchJob>(
                j => j.SendEventReminderAsync(toEmail, displayName, trip.Name, itemTitle, tripUrl), sendAt - now);

            db.Reminders.Add(new Reminder
            {
                Id = Guid.NewGuid(),
                ItineraryItemId = item.Id,
                UserId = userId,
                ScheduledForUtc = sendAt,
                HangfireJobId = jobId,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static TimeSpan ToTimeSpan(ReminderOffset offset) => offset switch
    {
        ReminderOffset.OneHour => TimeSpan.FromHours(1),
        ReminderOffset.TwelveHours => TimeSpan.FromHours(12),
        ReminderOffset.TwentyFourHours => TimeSpan.FromHours(24),
        _ => throw new ArgumentOutOfRangeException(nameof(offset), offset, null),
    };
}
