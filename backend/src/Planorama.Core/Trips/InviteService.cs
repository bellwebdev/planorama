using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Exceptions;
using Planorama.Core.Jobs;
using Planorama.Core.Options;

namespace Planorama.Core.Trips;

/// <inheritdoc cref="IInviteService"/>
public class InviteService(
    PlanoramaDbContext db,
    IBackgroundJobClient backgroundJobClient,
    IOptions<EmailOptions> emailOptions) : IInviteService
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(30);

    private readonly EmailOptions _email = emailOptions.Value;

    /// <inheritdoc/>
    public async Task<InviteResult> CreateInviteAsync(Guid tripId, Guid creatorId, InvitedVia via, string? contact, CancellationToken ct)
    {
        var trip = await db.Trips
            .Where(t => t.Id == tripId && t.Members.Any(m => m.UserId == creatorId && m.Status == TripMemberStatus.Accepted))
            .FirstOrDefaultAsync(ct)
            ?? throw new TripNotFoundException();

        if (trip.CreatorId != creatorId)
        {
            throw new ForbiddenException();
        }

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            InvitedVia = via,
            Contact = contact,
            ExpiresAt = DateTimeOffset.UtcNow.Add(InviteLifetime),
        };
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(ct);

        if (via == InvitedVia.Email && contact is not null)
        {
            var acceptUrl = $"{_email.InviteUrlBase}?token={invitation.Token}";
            backgroundJobClient.Enqueue<IEmailDispatchJob>(j => j.SendTripInviteAsync(contact, trip.Name, acceptUrl));
        }

        return new InviteResult(invitation.Token, invitation.InvitedVia, invitation.Contact, invitation.ExpiresAt);
    }

    /// <inheritdoc/>
    public async Task<TripResult> AcceptInviteAsync(Guid token, Guid userId, CancellationToken ct)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(i => i.Token == token, ct);
        if (invitation is null || invitation.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new InvalidInviteTokenException();
        }

        var member = await db.TripMembers.FindAsync([invitation.TripId, userId], ct);
        if (member is null)
        {
            member = new TripMember
            {
                TripId = invitation.TripId,
                UserId = userId,
                Role = TripMemberRole.Member,
                Status = TripMemberStatus.Accepted,
                InvitedVia = invitation.InvitedVia,
                InvitedContact = invitation.Contact,
                JoinedAt = DateTimeOffset.UtcNow,
            };
            db.TripMembers.Add(member);
            await db.SaveChangesAsync(ct);
        }
        else if (member.Status != TripMemberStatus.Accepted)
        {
            member.Status = TripMemberStatus.Accepted;
            member.JoinedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        var trip = await db.Trips.FirstAsync(t => t.Id == invitation.TripId, ct);
        return TripResult.FromEntity(trip);
    }
}
