namespace Planorama.Core.Integrations;

/// <summary>Tracks daily third-party API spend so a busy day degrades to cache-only rather than
/// silently blowing through the provider's free tier.</summary>
public interface IProviderQuotaGuard
{
    /// <summary>Records an intent to spend <paramref name="credits"/> against today's allowance.</summary>
    /// <param name="credits">Credits the pending call will consume.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when the call may proceed; <c>false</c> once the soft cap is reached.</returns>
    Task<bool> TryConsumeAsync(int credits, CancellationToken ct);
}
