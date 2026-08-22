namespace Planorama.Core.Itinerary;

/// <param name="Title">The suggestion's title — null only for a creator-pinned item with no suggestion.</param>
public record ItineraryItemResult(
    Guid Id,
    Guid TripId,
    Guid? SuggestionId,
    string? Title,
    string? Description,
    string? Address,
    double? Lat,
    double? Lng,
    DateOnly? Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int SortOrder,
    string? Timezone,
    DateTimeOffset CreatedAt);
