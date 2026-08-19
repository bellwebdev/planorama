using Planorama.Core.Domain;

namespace Planorama.Core.Trips;

public interface IInviteService
{
    /// <exception cref="Exceptions.TripNotFoundException">The trip doesn't exist, or the caller isn't an accepted member.</exception>
    /// <exception cref="Exceptions.ForbiddenException">The caller is a member but not the trip's creator.</exception>
    Task<InviteResult> CreateInviteAsync(Guid tripId, Guid creatorId, InvitedVia via, string? contact, CancellationToken ct);

    /// <summary>Idempotent — accepting an invite the caller has already used just returns the trip.</summary>
    /// <exception cref="Exceptions.InvalidInviteTokenException">The token doesn't exist or has expired.</exception>
    Task<TripResult> AcceptInviteAsync(Guid token, Guid userId, CancellationToken ct);
}
