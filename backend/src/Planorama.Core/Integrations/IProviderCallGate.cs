namespace Planorama.Core.Integrations;

/// <summary>Single choke point every outbound third-party call passes through: serve from cache,
/// otherwise check the daily quota before spending a request.</summary>
public interface IProviderCallGate
{
    /// <summary>Returns the cached value for <paramref name="cacheKey"/>, or invokes
    /// <paramref name="fetchAsync"/> and caches its result.</summary>
    /// <param name="cacheKey">Fully-qualified cache key, including the provider name.</param>
    /// <param name="ttl">How long a freshly fetched value stays cached.</param>
    /// <param name="credits">Credits the fetch will consume if it runs.</param>
    /// <param name="fetchAsync">The real provider call; invoked only on a cache miss.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached or freshly fetched value.</returns>
    /// <exception cref="Exceptions.ProviderQuotaExhaustedException">Cache miss while the daily quota is spent.</exception>
    Task<T?> GetOrFetchAsync<T>(string cacheKey, TimeSpan ttl, int credits, Func<CancellationToken, Task<T?>> fetchAsync, CancellationToken ct)
        where T : class;
}
