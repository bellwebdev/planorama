namespace Planorama.Core.Caching;

/// <summary>
/// Minimal key/value cache used to keep third-party provider calls inside their free tiers.
/// Implementations must degrade gracefully: a cache outage may slow the app down but must never
/// fail a request, so read/write failures are swallowed rather than thrown.
/// </summary>
public interface ICacheStore
{
    /// <summary>Reads and deserializes a cached value.</summary>
    /// <returns>The cached value, or <c>null</c> on a miss or a cache failure.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;

    /// <summary>Serializes and stores a value under <paramref name="key"/> for <paramref name="ttl"/>.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class;

    /// <summary>Atomically adds <paramref name="by"/> to a counter, applying <paramref name="ttl"/> on first write.</summary>
    /// <returns>The counter's new value.</returns>
    Task<long> IncrementAsync(string key, long by, TimeSpan ttl, CancellationToken ct);
}
