using Planorama.Api.Contracts.Trips;
using Planorama.Api.Validation;
using Xunit;

namespace Planorama.Tests.Unit;

public class CreateTripRequestValidatorTests
{
    private readonly CreateTripRequestValidator _validator = new();

    private static CreateTripRequest ValidRequest() => new(
        "Lake Trip", "Annual family trip", "Lake Tahoe", "123 Shore Rd",
        new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5), "America/Los_Angeles", 48);

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Day_trip_with_equal_start_and_end_date_passes()
    {
        var request = ValidRequest() with { EndDate = ValidRequest().StartDate };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_name_fails()
    {
        var request = ValidRequest() with { Name = "" };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void End_date_before_start_date_fails()
    {
        var request = ValidRequest() with { EndDate = ValidRequest().StartDate.AddDays(-1) };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Zero_voting_window_hours_fails()
    {
        var request = ValidRequest() with { DefaultVotingWindowHours = 0 };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Null_voting_window_hours_passes()
    {
        var request = ValidRequest() with { DefaultVotingWindowHours = null };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
