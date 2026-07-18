using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>
/// Stores <see cref="ReminderOffset"/> as the spec's text values ('1h' | '12h' | '24h').
/// </summary>
public class ReminderOffsetConverter : ValueConverter<ReminderOffset, string>
{
    public static readonly ReminderOffsetConverter Instance = new();

    public ReminderOffsetConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    private static string ToDb(ReminderOffset value) => value switch
    {
        ReminderOffset.OneHour => "1h",
        ReminderOffset.TwelveHours => "12h",
        ReminderOffset.TwentyFourHours => "24h",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static ReminderOffset FromDb(string value) => value switch
    {
        "1h" => ReminderOffset.OneHour,
        "12h" => ReminderOffset.TwelveHours,
        "24h" => ReminderOffset.TwentyFourHours,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
