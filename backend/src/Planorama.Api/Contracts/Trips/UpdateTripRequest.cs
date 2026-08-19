using Planorama.Core.Domain;

namespace Planorama.Api.Contracts.Trips;

public record UpdateTripRequest(
    string Name,
    string? Description,
    string LocationName,
    string StayAddress,
    DateOnly StartDate,
    DateOnly EndDate,
    string Timezone,
    int DefaultVotingWindowHours,
    TripStatus Status);
