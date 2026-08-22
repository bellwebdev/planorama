using Microsoft.EntityFrameworkCore;
using Planorama.Core.Data;
using Planorama.Core.Domain;

namespace Planorama.Core.Itinerary;

/// <summary>Keeps itinerary items in sync with a suggestion's resolution — called by both the
/// worker's automatic resolution and a creator's manual override (spec §6.5, §6.7). An approved
/// suggestion gets an item, at its proposed date/time if given or into the unscheduled tray
/// otherwise; anything else loses its item, since a discarded/vetoed suggestion has no business
/// staying on the itinerary.</summary>
public class ItinerarySyncService(PlanoramaDbContext db, ReminderScheduler reminders)
{
    public async Task SyncAsync(Suggestion suggestion, CancellationToken ct)
    {
        ItineraryItem? existing = await db.ItineraryItems.FirstOrDefaultAsync(i => i.SuggestionId == suggestion.Id, ct);

        if (suggestion.Status != SuggestionStatus.Approved)
        {
            if (existing is not null)
            {
                await reminders.CancelForItemAsync(existing.Id, ct);
                db.ItineraryItems.Remove(existing);
                await db.SaveChangesAsync(ct);
            }

            return;
        }

        if (existing is not null)
        {
            // Already on the itinerary — re-approving (e.g. an override flipping it back) is a no-op.
            return;
        }

        int lastSortOrder = await db.ItineraryItems
            .Where(i => i.TripId == suggestion.TripId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(ct) ?? -1;

        TimeOnly? endTime = suggestion is { ProposedStartTime: { } start, DurationMinutes: { } duration }
            ? start.Add(TimeSpan.FromMinutes(duration))
            : null;

        var item = new ItineraryItem
        {
            Id = Guid.NewGuid(),
            TripId = suggestion.TripId,
            SuggestionId = suggestion.Id,
            Date = suggestion.ProposedDate,
            StartTime = suggestion.ProposedStartTime,
            EndTime = endTime,
            SortOrder = lastSortOrder + 1,
        };
        db.ItineraryItems.Add(item);
        await db.SaveChangesAsync(ct);

        await reminders.RescheduleForItemAsync(item.Id, ct);
    }
}
