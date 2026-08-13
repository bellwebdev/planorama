using Planorama.Api.Contracts.Me;
using Planorama.Api.Validation;
using Planorama.Core.Domain;
using Xunit;

namespace Planorama.Tests.Unit;

public class UpdateSettingsRequestValidatorTests
{
    private readonly UpdateSettingsRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(new UpdateSettingsRequest(ReminderOffset.OneHour, true, false));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Out_of_range_reminder_offset_fails()
    {
        var result = _validator.Validate(new UpdateSettingsRequest((ReminderOffset)99, true, false));

        Assert.False(result.IsValid);
    }
}
