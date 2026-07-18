using Planorama.Core.Data;
using Planorama.Core.Domain;

namespace Planorama.Tests;

public class ReminderOffsetConverterTests
{
    [Theory]
    [InlineData(ReminderOffset.OneHour, "1h")]
    [InlineData(ReminderOffset.TwelveHours, "12h")]
    [InlineData(ReminderOffset.TwentyFourHours, "24h")]
    public void RoundTrips_spec_text_values(ReminderOffset offset, string dbValue)
    {
        var converter = ReminderOffsetConverter.Instance;

        var toDb = (string)converter.ConvertToProvider(offset)!;
        var fromDb = (ReminderOffset)converter.ConvertFromProvider(dbValue)!;

        Assert.Equal(dbValue, toDb);
        Assert.Equal(offset, fromDb);
    }

    [Fact]
    public void Rejects_unknown_db_value()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ReminderOffsetConverter.Instance.ConvertFromProvider("48h"));
    }
}
