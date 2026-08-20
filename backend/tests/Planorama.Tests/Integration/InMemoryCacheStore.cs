using System.Collections.Concurrent;
using Planorama.Core.Caching;

namespace Planorama.Tests.Integration;

/// <summary>Keeps the integration suite off Redis. TTLs are ignored — nothing in the endpoint tests
/// depends on expiry, and cache semantics are covered by the unit tests instead.</summary>
public class InMemoryCacheStore : ICacheStore
{
    private readonly ConcurrentDictionary<string, object> _entries = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class =>
        Task.FromResult(_entries.TryGetValue(key, out object? value) ? value as T : null);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class
    {
        _entries[key] = value;
        return Task.CompletedTask;
    }

    public Task<long> IncrementAsync(string key, long by, TimeSpan ttl, CancellationToken ct) =>
        Task.FromResult(_counters.AddOrUpdate(key, by, (_, current) => current + by));
}
