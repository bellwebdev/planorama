using Planorama.Core.Domain;

namespace Planorama.Core.Trips;

public static class TripQueryExtensions
{
    /// <summary>Restricts a trip query to trips the user is an accepted member of. Every
    /// trip-scoped read funnels through this so "not a member" and "doesn't exist" stay
    /// indistinguishable to the caller, and the rule lives in exactly one place.</summary>
    /// <param name="trips">The trip query to constrain.</param>
    /// <param name="userId">The calling user.</param>
    /// <returns>The query, filtered to that user's accepted memberships.</returns>
    public static IQueryable<Trip> AccessibleBy(this IQueryable<Trip> trips, Guid userId) =>
        trips.Where(t => t.Members.Any(m => m.UserId == userId && m.Status == TripMemberStatus.Accepted));
}
