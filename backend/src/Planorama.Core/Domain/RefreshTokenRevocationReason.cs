namespace Planorama.Core.Domain;

public enum RefreshTokenRevocationReason
{
    Rotated,
    ReuseDetected,
    UserLogout,

    /// <summary>Reserved for a future admin-revoke feature — not wired to any endpoint yet.</summary>
    AdminRevoked,
}
