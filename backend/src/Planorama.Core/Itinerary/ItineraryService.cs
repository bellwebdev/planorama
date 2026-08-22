using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Exceptions;
using Planorama.Core.Trips;

namespace Planorama.Core.Itinerary;

/// <inheritdoc cref="IItineraryService"/>
public class ItineraryService(PlanoramaDbContext db, ReminderScheduler reminders) : IItineraryService
{
    /// <summary>A shared, SQL-translatable projection — used by both the list and the post-update
    /// re-fetch, so the two can't drift.</summary>
    private static readonly Expression<Func<ItineraryItem, ItineraryItemResult>> ToResult = i => new ItineraryItemResult(
        i.Id, i.TripId, i.SuggestionId,
        i.Suggestion != null ? i.Suggestion.Title : null,
        i.Suggestion != null ? i.Suggestion.Description : null,
        i.Suggestion != null ? i.Suggestion.Address : null,
        i.Suggestion != null ? i.Suggestion.PlaceLat : null,
        i.Suggestion != null ? i.Suggestion.PlaceLng : null,
        i.Date, i.StartTime, i.EndTime, i.SortOrder, i.Timezone, i.CreatedAt);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItineraryItemResult>> ListForTripAsync(Guid tripId, Guid userId, CancellationToken ct)
    {
        bool isMember = await db.Trips.AccessibleBy(userId).AnyAsync(t => t.Id == tripId, ct);
        if (!isMember)
        {
            throw new TripNotFoundException();
        }

        // Scheduled items (Date != null) first, ordered by when they happen; unscheduled ones —
        // the creator's "unscheduled tray" — after, ordered by SortOrder alone.
        return await db.ItineraryItems
            .AsNoTracking()
            .Where(i => i.TripId == tripId)
            .OrderBy(i => i.Date == null)
            .ThenBy(i => i.Date)
            .ThenBy(i => i.StartTime)
            .ThenBy(i => i.SortOrder)
            .Select(ToResult)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<ItineraryItemResult> UpdateAsync(
        Guid itemId, Guid creatorId, DateOnly? date, TimeOnly? startTime, TimeOnly? endTime, int sortOrder, string? timezone, CancellationToken ct)
    {
        ItineraryItem item = await db.ItineraryItems
            .Where(i => i.Id == itemId)
            .FirstOrDefaultAsync(ct)
            ?? throw new ItineraryItemNotFoundException();

        Trip trip = await db.Trips.AccessibleBy(creatorId).Where(t => t.Id == item.TripId).FirstOrDefaultAsync(ct)
            ?? throw new ItineraryItemNotFoundException();

        if (trip.CreatorId != creatorId)
        {
            throw new ForbiddenException();
        }

        item.Date = date;
        item.StartTime = startTime;
        item.EndTime = endTime;
        item.SortOrder = sortOrder;
        item.Timezone = timezone;
        await db.SaveChangesAsync(ct);

        await reminders.RescheduleForItemAsync(item.Id, ct);

        return await db.ItineraryItems.AsNoTracking().Where(i => i.Id == item.Id).Select(ToResult).FirstAsync(ct);
    }
}
