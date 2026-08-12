namespace Planorama.Core.Domain;

/// <summary>
/// One row per issued token, rotated (not updated) on every refresh so the prior token stays
/// as an audit trail rather than being overwritten.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Shared by every token descended from one login; lets a stolen-token replay revoke the whole chain, not just the presented token.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>SHA-256 of the raw token — the raw value itself is never persisted.</summary>
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public RefreshTokenRevocationReason? RevocationReason { get; set; }

    /// <summary>Audit-only pointer to the token this row was rotated into; no FK constraint.</summary>
    public Guid? ReplacedByTokenId { get; set; }
}
