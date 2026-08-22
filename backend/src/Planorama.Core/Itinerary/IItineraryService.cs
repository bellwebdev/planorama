namespace Planorama.Core.Itinerary;

public interface IItineraryService
{
    /// <exception cref="Exceptions.TripNotFoundException">The trip doesn't exist, or the caller isn't an accepted member.</exception>
    Task<IReadOnlyList<ItineraryItemResult>> ListForTripAsync(Guid tripId, Guid userId, CancellationToken ct);

    /// <summary>Creator reorders/schedules an item (spec §7).</summary>
    /// <exception cref="Exceptions.ItineraryItemNotFoundException">No such item.</exception>
    /// <exception cref="Exceptions.ForbiddenException">The caller isn't the trip's creator.</exception>
    Task<ItineraryItemResult> UpdateAsync(
        Guid itemId, Guid creatorId, DateOnly? date, TimeOnly? startTime, TimeOnly? endTime, int sortOrder, string? timezone, CancellationToken ct);
}
