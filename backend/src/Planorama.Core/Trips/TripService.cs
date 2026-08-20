using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Exceptions;
using Planorama.Core.Integrations;

namespace Planorama.Core.Trips;

/// <inheritdoc cref="ITripService"/>
public class TripService(PlanoramaDbContext db, IGeocodingProvider geocoder, ILogger<TripService> logger) : ITripService
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
        await ApplyGeocodingAsync(trip, geocodeLocation: true, geocodeStay: true, ct);

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
                m.Trip.LocationLat,
                m.Trip.LocationLng,
                m.Trip.StayAddress,
                m.Trip.StayLat,
                m.Trip.StayLng,
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

        bool locationChanged = !string.Equals(trip.LocationName, locationName, StringComparison.Ordinal);
        bool stayChanged = !string.Equals(trip.StayAddress, stayAddress, StringComparison.Ordinal);

        trip.Name = name;
        trip.Description = description;
        trip.LocationName = locationName;
        trip.StayAddress = stayAddress;
        trip.StartDate = startDate;
        trip.EndDate = endDate;
        trip.Timezone = timezone;
        trip.DefaultVotingWindowHours = defaultVotingWindowHours;
        trip.Status = status;
        await ApplyGeocodingAsync(trip, locationChanged, stayChanged, ct);

        await db.SaveChangesAsync(ct);
        return TripResult.FromEntity(trip);
    }

    /// <summary>Single query collapses "trip doesn't exist" and "caller isn't an accepted member"
    /// into the same not-found outcome, so neither leaks a trip's existence to a non-member.</summary>
    private async Task<Trip> GetAccessibleTripAsync(Guid tripId, Guid userId, CancellationToken ct) =>
        await db.Trips
            .AccessibleBy(userId)
            .Where(t => t.Id == tripId)
            .FirstOrDefaultAsync(ct)
        ?? throw new TripNotFoundException();

    /// <summary>Resolves the trip's free-text addresses to coordinates, which places search and
    /// routing need as their origin. Only the fields whose text actually changed are re-resolved,
    /// so an unrelated edit (renaming the trip) doesn't spend provider credits.</summary>
    private async Task ApplyGeocodingAsync(Trip trip, bool geocodeLocation, bool geocodeStay, CancellationToken ct)
    {
        if (geocodeStay)
        {
            GeocodeResult? stay = await TryGeocodeAsync(trip.StayAddress, ct);
            trip.StayLat = stay?.Location.Latitude;
            trip.StayLng = stay?.Location.Longitude;
        }

        if (geocodeLocation)
        {
            GeocodeResult? location = await TryGeocodeAsync(trip.LocationName, ct);
            trip.LocationLat = location?.Location.Latitude;
            trip.LocationLng = location?.Location.Longitude;
        }
    }

    /// <summary>Best-effort by design: a geocoding outage or an unrecognisable address must not
    /// block saving a trip, so failures leave the coordinate null. Place search reports the gap
    /// separately via <see cref="TripNotGeocodedException"/> rather than failing here.</summary>
    private async Task<GeocodeResult?> TryGeocodeAsync(string address, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        try
        {
            return await geocoder.GeocodeAsync(address, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Address text is deliberately not logged — it is user PII and the exception is enough to diagnose.
            logger.LogWarning(ex, "Geocoding failed; trip coordinate left unresolved");
            return null;
        }
    }
}
