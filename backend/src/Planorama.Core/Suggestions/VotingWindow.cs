namespace Planorama.Core.Suggestions;

/// <summary>
/// Spec §6.1's voting-window rule, as a pure function so the Phase 2 worker resolves against
/// exactly the same arithmetic that created the window.
/// </summary>
public static class VotingWindow
{
    /// <summary>Voting must finish this far ahead of the event so members can actually act on the result.</summary>
    private static readonly TimeSpan PreEventBuffer = TimeSpan.FromHours(12);

    /// <summary>Floor on the window. The §6.1 clamp lands in the past for a trip starting within
    /// 12 hours; rather than reject a day-of suggestion outright, it gets a short real window.</summary>
    private static readonly TimeSpan MinimumWindow = TimeSpan.FromHours(1);

    /// <summary>Calculates when voting closes for a new suggestion.</summary>
    /// <param name="now">Current instant (UTC).</param>
    /// <param name="requestedClose">Deadline the suggester asked for, if any.</param>
    /// <param name="tripDefaultWindowHours">The trip's default window, used when no deadline was requested.</param>
    /// <param name="proposedDate">The suggestion's proposed date, if any.</param>
    /// <param name="proposedStartTime">The proposed start time, if any.</param>
    /// <param name="tripStartDate">The trip's start date, used as the anchor when no date was proposed.</param>
    /// <param name="tripTimezone">Zone the proposed/trip dates are expressed in.</param>
    /// <returns>The instant voting closes, always at least <see cref="MinimumWindow"/> away.</returns>
    public static DateTimeOffset Calculate(
        DateTimeOffset now,
        DateTimeOffset? requestedClose,
        int tripDefaultWindowHours,
        DateOnly? proposedDate,
        TimeOnly? proposedStartTime,
        DateOnly tripStartDate,
        TimeZoneInfo tripTimezone)
    {
        DateTimeOffset target = requestedClose ?? now.AddHours(tripDefaultWindowHours);

        // Anchor on the proposed start time when there is one — "12h before the event" is the
        // intent, and midnight-of-the-date would close voting a day and a half early.
        DateOnly anchorDate = proposedDate ?? tripStartDate;
        TimeOnly anchorTime = proposedDate is null ? TimeOnly.MinValue : proposedStartTime ?? TimeOnly.MinValue;
        DateTimeOffset latestAllowed = TripTimeZone.ToUtcInstant(anchorDate, anchorTime, tripTimezone) - PreEventBuffer;

        DateTimeOffset clamped = target < latestAllowed ? target : latestAllowed;
        DateTimeOffset floor = now + MinimumWindow;

        return clamped > floor ? clamped : floor;
    }
}
