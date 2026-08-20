using Planorama.Core.Domain;

namespace Planorama.Core.Trips;

public interface ITripService
{
    Task<TripResult> CreateAsync(
        Guid creatorId, string name, string? description, string locationName, string stayAddress,
        DateOnly startDate, DateOnly endDate, string timezone, int defaultVotingWindowHours, CancellationToken ct);

    /// <exception cref="Exceptions.TripNotFoundException">The trip doesn't exist, or the caller isn't an accepted member.</exception>
    Task<TripResult> GetByIdAsync(Guid tripId, Guid userId, CancellationToken ct);

    Task<IReadOnlyList<TripResult>> ListForUserAsync(Guid userId, CancellationToken ct);

    /// <exception cref="Exceptions.TripNotFoundException">The trip doesn't exist, or the caller isn't an accepted member.</exception>
    /// <exception cref="Exceptions.ForbiddenException">The caller is a member but not the trip's creator.</exception>
    Task<TripResult> UpdateAsync(
        Guid tripId, Guid userId, string name, string? description, string locationName, string stayAddress,
        DateOnly startDate, DateOnly endDate, string timezone, int defaultVotingWindowHours, TripStatus status, CancellationToken ct);
}
