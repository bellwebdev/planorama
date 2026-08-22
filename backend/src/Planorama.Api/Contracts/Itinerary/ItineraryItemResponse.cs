using Planorama.Core.Itinerary;

namespace Planorama.Api.Contracts.Itinerary;

public record ItineraryItemResponse(
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
    DateTimeOffset CreatedAt)
{
    public static ItineraryItemResponse FromResult(ItineraryItemResult r) => new(
        r.Id, r.TripId, r.SuggestionId, r.Title, r.Description, r.Address, r.Lat, r.Lng,
        r.Date, r.StartTime, r.EndTime, r.SortOrder, r.Timezone, r.CreatedAt);
}
