using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>Stores <see cref="TripMemberStatus"/> as text ('invited' | 'accepted' | 'declined').</summary>
public class TripMemberStatusConverter : ValueConverter<TripMemberStatus, string>
{
    public static readonly TripMemberStatusConverter Instance = new();

    public TripMemberStatusConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    private static string ToDb(TripMemberStatus value) => value switch
    {
        TripMemberStatus.Invited => "invited",
        TripMemberStatus.Accepted => "accepted",
        TripMemberStatus.Declined => "declined",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static TripMemberStatus FromDb(string value) => value switch
    {
        "invited" => TripMemberStatus.Invited,
        "accepted" => TripMemberStatus.Accepted,
        "declined" => TripMemberStatus.Declined,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
