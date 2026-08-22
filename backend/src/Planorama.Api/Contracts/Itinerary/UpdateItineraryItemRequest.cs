namespace Planorama.Api.Contracts.Itinerary;

public record UpdateItineraryItemRequest(DateOnly? Date, TimeOnly? StartTime, TimeOnly? EndTime, int SortOrder, string? Timezone);
