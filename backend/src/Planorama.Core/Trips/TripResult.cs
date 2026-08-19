using Planorama.Core.Domain;

namespace Planorama.Core.Trips;

public record TripResult(
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
    public static TripResult FromEntity(Trip trip) => new(
        trip.Id,
        trip.CreatorId,
        trip.Name,
        trip.Description,
        trip.LocationName,
        trip.StayAddress,
        trip.StartDate,
        trip.EndDate,
        trip.Timezone,
        trip.Status,
        trip.DefaultVotingWindowHours,
        trip.CreatedAt);
}
