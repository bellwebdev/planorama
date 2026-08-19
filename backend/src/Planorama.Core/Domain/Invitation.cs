namespace Planorama.Core.Domain;

/// <summary>
/// A shareable capability to join a trip, keyed by <see cref="Token"/> — not consumed on accept,
/// so a link invite can be used by multiple people and an email invite can be safely re-clicked.
/// No revoke/status tracking in 1a; validity is just "not expired" (see <see cref="ExpiresAt"/>).
/// </summary>
public class Invitation
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid Token { get; set; } = Guid.NewGuid();
    public InvitedVia InvitedVia { get; set; }

    /// <summary>Email address for an email invite; null for a shareable link invite.</summary>
    public string? Contact { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Trip? Trip { get; set; }
}
