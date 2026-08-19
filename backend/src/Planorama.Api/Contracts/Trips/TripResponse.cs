using Planorama.Core.Domain;
using Planorama.Core.Trips;

namespace Planorama.Api.Contracts.Trips;

public record TripResponse(
    Guid Id,
    Guid CreatorId,
    string Name,
    string? Description,
    string LocationName,
    string StayAddress,
    DateOnly StartDate,
    DateOnly EndDate,
    string Timezone,
    TripStatus Status,
    int DefaultVotingWindowHours,
    DateTimeOffset CreatedAt)
{
    public static TripResponse FromResult(TripResult trip) => new(
        trip.Id, trip.CreatorId, trip.Name, trip.Description, trip.LocationName, trip.StayAddress,
        trip.StartDate, trip.EndDate, trip.Timezone, trip.Status, trip.DefaultVotingWindowHours, trip.CreatedAt);
}
