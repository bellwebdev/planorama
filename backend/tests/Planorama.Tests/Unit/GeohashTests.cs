using Planorama.Core.Integrations;
using Xunit;

namespace Planorama.Tests.Unit;

public class GeohashTests
{
    [Theory]
    // The canonical worked example from the geohash specification.
    [InlineData(57.64911, 10.40744, 5, "u4pru")]
    [InlineData(57.64911, 10.40744, 11, "u4pruydqqvj")]
    [InlineData(0, 0, 5, "s0000")]
    [InlineData(-33.8688, 151.2093, 5, "r3gx2")]
    public void Encodes_known_coordinates(double latitude, double longitude, int precision, string expected) =>
        Assert.Equal(expected, Geohash.Encode(latitude, longitude, precision));

    [Fact]
    public void Nearby_coordinates_share_a_cell_so_they_share_a_cache_entry()
    {
        var stay = new GeoPoint(51.5074, -0.1278);
        var fewHundredMetresAway = new GeoPoint(51.5081, -0.1265);

        Assert.Equal(Geohash.Encode(stay), Geohash.Encode(fewHundredMetresAway));
    }

    [Fact]
    public void Distant_coordinates_do_not_share_a_cell() =>
        Assert.NotEqual(Geohash.Encode(51.5074, -0.1278), Geohash.Encode(48.8566, 2.3522));
}
