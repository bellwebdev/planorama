namespace Planorama.Api.Contracts.Trips;

public record CreateTripRequest(
    string Name,
    string? Description,
    string LocationName,
    string StayAddress,
    DateOnly StartDate,
    DateOnly EndDate,
    string Timezone,
    int? DefaultVotingWindowHours);
