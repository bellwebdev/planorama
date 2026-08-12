using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>
/// Stores <see cref="RefreshTokenRevocationReason"/> as text ('rotated' | 'reuse_detected' | 'user_logout' | 'admin_revoked').
/// </summary>
public class RefreshTokenRevocationReasonConverter : ValueConverter<RefreshTokenRevocationReason, string>
{
    public static readonly RefreshTokenRevocationReasonConverter Instance = new();

    public RefreshTokenRevocationReasonConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    private static string ToDb(RefreshTokenRevocationReason value) => value switch
    {
        RefreshTokenRevocationReason.Rotated => "rotated",
        RefreshTokenRevocationReason.ReuseDetected => "reuse_detected",
        RefreshTokenRevocationReason.UserLogout => "user_logout",
        RefreshTokenRevocationReason.AdminRevoked => "admin_revoked",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static RefreshTokenRevocationReason FromDb(string value) => value switch
    {
        "rotated" => RefreshTokenRevocationReason.Rotated,
        "reuse_detected" => RefreshTokenRevocationReason.ReuseDetected,
        "user_logout" => RefreshTokenRevocationReason.UserLogout,
        "admin_revoked" => RefreshTokenRevocationReason.AdminRevoked,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
