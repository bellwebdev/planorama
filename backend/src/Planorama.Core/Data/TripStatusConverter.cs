using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>Stores <see cref="TripStatus"/> as text ('draft' | 'planning' | 'active' | 'completed').</summary>
public class TripStatusConverter : ValueConverter<TripStatus, string>
{
    public static readonly TripStatusConverter Instance = new();

    public TripStatusConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    private static string ToDb(TripStatus value) => value switch
    {
        TripStatus.Draft => "draft",
        TripStatus.Planning => "planning",
        TripStatus.Active => "active",
        TripStatus.Completed => "completed",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static TripStatus FromDb(string value) => value switch
    {
        "draft" => TripStatus.Draft,
        "planning" => TripStatus.Planning,
        "active" => TripStatus.Active,
        "completed" => TripStatus.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
