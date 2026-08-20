namespace Planorama.Core.Integrations;

/// <summary>
/// Geohash encoding, used only to build cache keys: it collapses near-identical coordinates onto
/// one key so a member panning the map a few metres reuses a cached provider response instead of
/// spending quota. Precision 5 is roughly a 5km cell (see <c>system-design.md</c> §4.2).
/// </summary>
public static class Geohash
{
    private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

    /// <summary>Encodes a coordinate to a geohash string.</summary>
    /// <param name="latitude">Latitude in degrees.</param>
    /// <param name="longitude">Longitude in degrees.</param>
    /// <param name="precision">Number of characters to emit; higher is a smaller cell.</param>
    /// <returns>The geohash of the coordinate.</returns>
    public static string Encode(double latitude, double longitude, int precision = 5)
    {
        // Comparisons are >=, not >, so a coordinate sitting exactly on a cell boundary rounds the
        // same way geohash.org and the reference implementations do.
        double latMin = -90d, latMax = 90d, lonMin = -180d, lonMax = 180d;
        var hash = new System.Text.StringBuilder(precision);
        var bit = 0;
        var index = 0;
        var evenBit = true;

        while (hash.Length < precision)
        {
            if (evenBit)
            {
                double lonMid = (lonMin + lonMax) / 2;
                if (longitude >= lonMid)
                {
                    index = (index << 1) | 1;
                    lonMin = lonMid;
                }
                else
                {
                    index <<= 1;
                    lonMax = lonMid;
                }
            }
            else
            {
                double latMid = (latMin + latMax) / 2;
                if (latitude >= latMid)
                {
                    index = (index << 1) | 1;
                    latMin = latMid;
                }
                else
                {
                    index <<= 1;
                    latMax = latMid;
                }
            }

            evenBit = !evenBit;
            if (++bit == 5)
            {
                hash.Append(Base32[index]);
                bit = 0;
                index = 0;
            }
        }

        return hash.ToString();
    }

    /// <inheritdoc cref="Encode(double, double, int)"/>
    public static string Encode(GeoPoint point, int precision = 5) => Encode(point.Latitude, point.Longitude, precision);
}
