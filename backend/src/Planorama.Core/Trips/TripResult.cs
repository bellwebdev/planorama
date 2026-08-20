using Planorama.Core.Domain;

namespace Planorama.Core.Trips;

public record TripResult(
    Guid Id,
    Guid CreatorId,
    string Name,
    string? Description,
    string LocationName,
    double? LocationLat,
    double? LocationLng,
    string StayAddress,
    double? StayLat,
    double? StayLng,
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
        trip.LocationLat,
        trip.LocationLng,
        trip.StayAddress,
        trip.StayLat,
        trip.StayLng,
        trip.StartDate,
        trip.EndDate,
        trip.Timezone,
        trip.Status,
        trip.DefaultVotingWindowHours,
        trip.CreatedAt);
}
