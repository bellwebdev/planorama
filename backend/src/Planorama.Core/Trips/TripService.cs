using Microsoft.EntityFrameworkCore;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Exceptions;

namespace Planorama.Core.Trips;

/// <inheritdoc cref="ITripService"/>
public class TripService(PlanoramaDbContext db) : ITripService
{
    /// <inheritdoc/>
    public async Task<TripResult> CreateAsync(
        Guid creatorId, string name, string? description, string locationName, string stayAddress,
        DateOnly startDate, DateOnly endDate, string timezone, int defaultVotingWindowHours, CancellationToken ct)
    {
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorId,
            Name = name,
            Description = description,
            LocationName = locationName,
            StayAddress = stayAddress,
            StartDate = startDate,
            EndDate = endDate,
            Timezone = timezone,
            DefaultVotingWindowHours = defaultVotingWindowHours,
        };
        db.Trips.Add(trip);
        db.TripMembers.Add(new TripMember
        {
            TripId = trip.Id,
            UserId = creatorId,
            Role = TripMemberRole.Creator,
            Status = TripMemberStatus.Accepted,
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return TripResult.FromEntity(trip);
    }

    /// <inheritdoc/>
    public async Task<TripResult> GetByIdAsync(Guid tripId, Guid userId, CancellationToken ct) =>
        TripResult.FromEntity(await GetAccessibleTripAsync(tripId, userId, ct));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TripResult>> ListForUserAsync(Guid userId, CancellationToken ct) =>
        await db.TripMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == TripMemberStatus.Accepted)
            .Select(m => new TripResult(
                m.Trip!.Id,
                m.Trip.CreatorId,
                m.Trip.Name,
                m.Trip.Description,
                m.Trip.LocationName,
                m.Trip.StayAddress,
                m.Trip.StartDate,
                m.Trip.EndDate,
                m.Trip.Timezone,
                m.Trip.Status,
                m.Trip.DefaultVotingWindowHours,
                m.Trip.CreatedAt))
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<TripResult> UpdateAsync(
        Guid tripId, Guid userId, string name, string? description, string locationName, string stayAddress,
        DateOnly startDate, DateOnly endDate, string timezone, int defaultVotingWindowHours, TripStatus status, CancellationToken ct)
    {
        var trip = await GetAccessibleTripAsync(tripId, userId, ct);
        if (trip.CreatorId != userId)
        {
            throw new ForbiddenException();
        }

        trip.Name = name;
        trip.Description = description;
        trip.LocationName = locationName;
        trip.StayAddress = stayAddress;
        trip.StartDate = startDate;
        trip.EndDate = endDate;
        trip.Timezone = timezone;
        trip.DefaultVotingWindowHours = defaultVotingWindowHours;
        trip.Status = status;
        await db.SaveChangesAsync(ct);
        return TripResult.FromEntity(trip);
    }

    /// <summary>Single query collapses "trip doesn't exist" and "caller isn't an accepted member"
    /// into the same not-found outcome, so neither leaks a trip's existence to a non-member.</summary>
    private async Task<Trip> GetAccessibleTripAsync(Guid tripId, Guid userId, CancellationToken ct) =>
        await db.Trips
            .Where(t => t.Id == tripId && t.Members.Any(m => m.UserId == userId && m.Status == TripMemberStatus.Accepted))
            .FirstOrDefaultAsync(ct)
        ?? throw new TripNotFoundException();
}
