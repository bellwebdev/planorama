using Planorama.Core.Integrations;
using Planorama.Core.Places;

namespace Planorama.Api.Places;

/// <summary>
/// Cache-and-quota decorator around a real <see cref="IPlacesProvider"/>. Kept separate from the
/// provider so the adapter stays pure HTTP-and-parsing, and so the caching rules are testable
/// without a network.
/// </summary>
/// <param name="providerKey">Cache-key namespace, so two providers never share cached entries.</param>
/// <param name="inner">The provider actually making the calls.</param>
/// <param name="gate">Shared cache/quota choke point.</param>
public class CachingPlacesProvider(string providerKey, IPlacesProvider inner, IProviderCallGate gate) : IPlacesProvider
{
    /// <summary>Shorter than detail: a search's result set changes as places open and close, and
    /// searches are the highest-volume call.</summary>
    private static readonly TimeSpan SearchTtl = TimeSpan.FromHours(6);

    /// <summary>24h per <c>system-design.md</c> §4.1.</summary>
    private static readonly TimeSpan DetailTtl = TimeSpan.FromHours(24);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlaceResult>> SearchNearbyAsync(PlaceSearchQuery query, CancellationToken ct) =>
        await gate.GetOrFetchAsync(
            $"{providerKey}:places:search:{query.CacheFragment}",
            SearchTtl,
            // Geoapify bills roughly one credit per 20 places returned.
            credits: Math.Max(1, (int)Math.Ceiling(query.Limit / 20d)),
            async token => (await inner.SearchNearbyAsync(query, token)).ToList(),
            ct)
        ?? [];

    /// <inheritdoc/>
    public Task<PlaceDetail?> GetDetailAsync(string providerPlaceId, CancellationToken ct) =>
        gate.GetOrFetchAsync(
            $"{providerKey}:places:detail:{providerPlaceId}",
            DetailTtl,
            credits: 1,
            token => inner.GetDetailAsync(providerPlaceId, token),
            ct);
}
