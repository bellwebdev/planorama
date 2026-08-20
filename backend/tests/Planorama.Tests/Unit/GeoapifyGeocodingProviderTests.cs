using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Api.Places;
using Planorama.Core.Integrations;
using Xunit;

namespace Planorama.Tests.Unit;

public class GeoapifyGeocodingProviderTests
{
    [Fact]
    public async Task Maps_the_first_result_including_its_timezone()
    {
        const string body = """
        { "results": [ { "lat": 51.5074, "lon": -0.1278, "formatted": "1 Dock Rd, London",
                         "timezone": { "name": "Europe/London" } } ] }
        """;

        GeocodeResult? result = await CreateProvider(body).GeocodeAsync("1 Dock Rd", default);

        Assert.NotNull(result);
        Assert.Equal(51.5074, result!.Location.Latitude);
        Assert.Equal(-0.1278, result.Location.Longitude);
        Assert.Equal("1 Dock Rd, London", result.FormattedAddress);
        Assert.Equal("Europe/London", result.Timezone);
    }

    [Fact]
    public async Task Returns_null_when_the_address_cannot_be_resolved() =>
        Assert.Null(await CreateProvider("""{"results":[]}""").GeocodeAsync("nowhere at all", default));

    private static GeoapifyGeocodingProvider CreateProvider(string body) =>
        new(new HttpClient(new StubHttpMessageHandler(System.Net.HttpStatusCode.OK, body)),
            Options.Create(new GeoapifyOptions { ApiKey = "test-key" }));
}
