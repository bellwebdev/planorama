using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>Stores <see cref="TripMemberRole"/> as text ('creator' | 'member').</summary>
public class TripMemberRoleConverter : ValueConverter<TripMemberRole, string>
{
    public static readonly TripMemberRoleConverter Instance = new();

    public TripMemberRoleConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    private static string ToDb(TripMemberRole value) => value switch
    {
        TripMemberRole.Creator => "creator",
        TripMemberRole.Member => "member",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static TripMemberRole FromDb(string value) => value switch
    {
        "creator" => TripMemberRole.Creator,
        "member" => TripMemberRole.Member,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
