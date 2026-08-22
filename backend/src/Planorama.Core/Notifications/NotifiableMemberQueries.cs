using Microsoft.EntityFrameworkCore;
using Planorama.Core.Data;
using Planorama.Core.Domain;

namespace Planorama.Core.Notifications;

/// <summary>Shared by every trip-event notification (suggestion-added, vote-result, …): who gets
/// emailed. A member with no <see cref="UserSettings"/> row yet has never touched their
/// preferences, so the entity's default (opted in) applies.</summary>
public static class NotifiableMemberQueries
{
    public static IQueryable<(string Email, string DisplayName)> NotifiableMembers(
        this PlanoramaDbContext db, Guid tripId, Guid? excludeUserId = null)
    {
        IQueryable<TripMember> members = db.TripMembers.Where(m => m.TripId == tripId && m.Status == TripMemberStatus.Accepted);
        if (excludeUserId is { } exclude)
        {
            members = members.Where(m => m.UserId != exclude);
        }

        return members
            .Join(db.Users, m => m.UserId, u => u.Id, (_, u) => u)
            .Where(u => u.Settings == null || u.Settings.NotifyEmail)
            .Select(u => new ValueTuple<string, string>(u.Email!, u.DisplayName));
    }
}
