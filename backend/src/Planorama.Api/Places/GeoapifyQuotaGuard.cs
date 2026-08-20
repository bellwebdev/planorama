using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Core.Caching;
using Planorama.Core.Integrations;

namespace Planorama.Api.Places;

/// <inheritdoc cref="IProviderQuotaGuard"/>
public class GeoapifyQuotaGuard(ICacheStore cache, IOptions<GeoapifyOptions> options, ILogger<GeoapifyQuotaGuard> logger)
    : IProviderQuotaGuard
{
    /// <summary>Counters outlive the day they measure by a wide margin so a clock skew or a slow
    /// request can't resurrect a stale key as "today".</summary>
    private static readonly TimeSpan CounterTtl = TimeSpan.FromHours(48);

    private readonly GeoapifyOptions _geoapify = options.Value;

    /// <inheritdoc/>
    public async Task<bool> TryConsumeAsync(int credits, CancellationToken ct)
    {
        // Geoapify's allowance resets at UTC midnight, so the key is bucketed the same way.
        string key = $"geoapify:quota:{DateTime.UtcNow:yyyy-MM-dd}";
        long used = await cache.IncrementAsync(key, credits, CounterTtl, ct);
        var softCap = (long)(_geoapify.DailyCreditCap * _geoapify.SoftCapFraction);

        if (used <= softCap)
        {
            return true;
        }

        logger.LogWarning("Geoapify daily quota soft cap reached ({Used}/{SoftCap} credits); serving cache only", used, softCap);
        return false;
    }
}
