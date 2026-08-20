using Planorama.Core.Caching;
using Planorama.Core.Exceptions;

namespace Planorama.Core.Integrations;

/// <inheritdoc cref="IProviderCallGate"/>
public class ProviderCallGate(ICacheStore cache, IProviderQuotaGuard quota) : IProviderCallGate
{
    /// <inheritdoc/>
    public async Task<T?> GetOrFetchAsync<T>(
        string cacheKey, TimeSpan ttl, int credits, Func<CancellationToken, Task<T?>> fetchAsync, CancellationToken ct)
        where T : class
    {
        // Cache is consulted before the quota check on purpose: once the daily allowance is spent,
        // already-cached answers keep working instead of the whole feature going dark.
        T? cached = await cache.GetAsync<T>(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        if (!await quota.TryConsumeAsync(credits, ct))
        {
            throw new ProviderQuotaExhaustedException();
        }

        T? fresh = await fetchAsync(ct);
        if (fresh is not null)
        {
            await cache.SetAsync(cacheKey, fresh, ttl, ct);
        }

        return fresh;
    }
}
