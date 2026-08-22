using Microsoft.Extensions.Logging;

namespace Planorama.Core;

/// <summary>Shared IANA-timezone plumbing — every wall-clock-to-UTC conversion in the domain goes
/// through this, so "resolve a trip/item timezone, convert, do math in UTC" stays the one rule
/// (never a "trip local time" concept) instead of being reimplemented per feature.</summary>
public static class TripTimeZone
{
    /// <summary>Trip timezones are stored as free text, so an unusable id must not take the caller
    /// down — UTC keeps the math sane and is logged for follow-up.</summary>
    public static TimeZoneInfo Resolve(string timezoneId, ILogger logger)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning(ex, "Unrecognised trip timezone {Timezone}; falling back to UTC", timezoneId);
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>Resolves a wall-clock date/time in the given zone to a UTC instant.</summary>
    public static DateTimeOffset ToUtcInstant(DateOnly date, TimeOnly time, TimeZoneInfo timezone)
    {
        var local = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, timezone.GetUtcOffset(local)).ToUniversalTime();
    }
}
