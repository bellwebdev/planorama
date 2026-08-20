namespace Planorama.Core.Domain;

public class TripMember
{
    public Guid TripId { get; set; }
    public Guid UserId { get; set; }
    public TripMemberRole Role { get; set; }
    public TripMemberStatus Status { get; set; }
    public InvitedVia? InvitedVia { get; set; }
    public string? InvitedContact { get; set; }
    public DateTimeOffset? JoinedAt { get; set; }

    public Trip? Trip { get; set; }
}
