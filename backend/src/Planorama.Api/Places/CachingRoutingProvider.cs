using Planorama.Core.Exceptions;
using Planorama.Core.Integrations;

namespace Planorama.Api.Places;

/// <inheritdoc cref="CachingPlacesProvider"/>
public class CachingRoutingProvider(string providerKey, IRoutingProvider inner, IProviderCallGate gate) : IRoutingProvider
{
    /// <summary>7d per <c>system-design.md</c> §4.1 — routes between two fixed points barely change.</summary>
    private static readonly TimeSpan RouteTtl = TimeSpan.FromDays(7);

    /// <summary>Precision 7 is a ~150m cell: fine enough that distinct destinations keep distinct
    /// routes, coarse enough that re-picking the same place reuses the cached answer.</summary>
    private const int RouteGeohashPrecision = 7;

    /// <inheritdoc/>
    public async Task<RouteResult> GetRouteAsync(GeoPoint from, GeoPoint to, TravelMode mode, CancellationToken ct)
    {
        string key = $"{providerKey}:route:{mode}:{Geohash.Encode(from, RouteGeohashPrecision)}:{Geohash.Encode(to, RouteGeohashPrecision)}";

        return await gate.GetOrFetchAsync(key, RouteTtl, credits: 1, token => WrapAsync(inner.GetRouteAsync(from, to, mode, token)), ct)
            ?? throw new ProviderUnavailableException();
    }

    /// <summary>The gate deals in nullable results; the routing contract doesn't. A missing route
    /// arrives as <see cref="RouteNotFoundException"/> from the adapter, never as null.</summary>
    private static async Task<RouteResult?> WrapAsync(Task<RouteResult> call) => await call;
}
