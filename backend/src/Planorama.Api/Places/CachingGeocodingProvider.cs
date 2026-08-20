using System.Security.Cryptography;
using System.Text;
using Planorama.Core.Integrations;

namespace Planorama.Api.Places;

/// <inheritdoc cref="CachingPlacesProvider"/>
public class CachingGeocodingProvider(string providerKey, IGeocodingProvider inner, IProviderCallGate gate) : IGeocodingProvider
{
    /// <summary>Addresses don't move. Long TTL, since this is spent on every trip create and edit.</summary>
    private static readonly TimeSpan GeocodeTtl = TimeSpan.FromDays(30);

    /// <inheritdoc/>
    public Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct) =>
        gate.GetOrFetchAsync(
            $"{providerKey}:geocode:{HashAddress(address)}",
            GeocodeTtl,
            credits: 1,
            token => inner.GeocodeAsync(address, token),
            ct);

    /// <summary>Addresses are free text — hashing keeps keys bounded and avoids writing user PII
    /// into Redis in the clear. Normalised first so casing and padding don't fragment the cache.</summary>
    private static string HashAddress(string address) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(address.Trim().ToLowerInvariant())))[..16];
}
