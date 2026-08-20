using Planorama.Core.Suggestions;
using Xunit;

namespace Planorama.Tests.Unit;

public class VotingWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [Fact]
    public void Defaults_to_the_trips_window_when_no_deadline_is_requested()
    {
        DateTimeOffset closes = VotingWindow.Calculate(
            Now, requestedClose: null, tripDefaultWindowHours: 48,
            proposedDate: null, proposedStartTime: null,
            tripStartDate: new DateOnly(2026, 8, 1), Utc);

        Assert.Equal(Now.AddHours(48), closes);
    }

    [Fact]
    public void Honours_a_requested_deadline_inside_the_allowed_range()
    {
        DateTimeOffset requested = Now.AddHours(6);

        DateTimeOffset closes = VotingWindow.Calculate(
            Now, requested, tripDefaultWindowHours: 48,
            proposedDate: null, proposedStartTime: null,
            tripStartDate: new DateOnly(2026, 8, 1), Utc);

        Assert.Equal(requested, closes);
    }

    [Fact]
    public void Clamps_to_twelve_hours_before_the_proposed_start_time()
    {
        // Event at 18:00 on 3 July → voting must close by 06:00 on 3 July, well inside the 48h default.
        DateTimeOffset closes = VotingWindow.Calculate(
            Now, requestedClose: null, tripDefaultWindowHours: 48,
            proposedDate: new DateOnly(2026, 7, 3), proposedStartTime: new TimeOnly(18, 0),
            tripStartDate: new DateOnly(2026, 7, 1), Utc);

        Assert.Equal(new DateTimeOffset(2026, 7, 3, 6, 0, 0, TimeSpan.Zero), closes);
    }

    [Fact]
    public void Clamps_against_the_trip_start_when_no_date_is_proposed()
    {
        // Trip starts midnight 2 July → voting must close by 12:00 on 1 July, i.e. right now.
        DateTimeOffset closes = VotingWindow.Calculate(
            Now, requestedClose: null, tripDefaultWindowHours: 48,
            proposedDate: null, proposedStartTime: null,
            tripStartDate: new DateOnly(2026, 7, 2), Utc);

        // Exactly at the floor rather than the clamp, since the clamp lands on `now`.
        Assert.Equal(Now.AddHours(1), closes);
    }

    [Fact]
    public void Floors_the_window_at_one_hour_when_the_clamp_lands_in_the_past()
    {
        // The trip already started — a strict §6.1 clamp would produce a deadline days ago.
        DateTimeOffset closes = VotingWindow.Calculate(
            Now, requestedClose: null, tripDefaultWindowHours: 48,
            proposedDate: null, proposedStartTime: null,
            tripStartDate: new DateOnly(2026, 6, 28), Utc);

        Assert.Equal(Now.AddHours(1), closes);
    }

    [Fact]
    public void Floors_a_requested_deadline_that_is_already_in_the_past()
    {
        DateTimeOffset closes = VotingWindow.Calculate(
            Now, requestedClose: Now.AddHours(-5), tripDefaultWindowHours: 48,
            proposedDate: null, proposedStartTime: null,
            tripStartDate: new DateOnly(2026, 8, 1), Utc);

        Assert.Equal(Now.AddHours(1), closes);
    }

    [Fact]
    public void Resolves_the_proposed_date_in_the_trips_timezone_not_utc()
    {
        var newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        // 3 July 18:00 in New York (EDT, UTC-4) is 3 July 22:00 UTC → close at 10:00 UTC.
        DateTimeOffset closes = VotingWindow.Calculate(
            Now, requestedClose: null, tripDefaultWindowHours: 48,
            proposedDate: new DateOnly(2026, 7, 3), proposedStartTime: new TimeOnly(18, 0),
            tripStartDate: new DateOnly(2026, 7, 1), newYork);

        Assert.Equal(new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero), closes);
    }
}
