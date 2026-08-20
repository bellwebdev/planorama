using Planorama.Core.Exceptions;
using Planorama.Core.Integrations;
using Planorama.Tests.Integration;
using Xunit;

namespace Planorama.Tests.Unit;

public class ProviderCallGateTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    [Fact]
    public async Task Fetches_and_caches_on_a_miss()
    {
        var cache = new InMemoryCacheStore();
        var quota = new CountingQuotaGuard(allow: true);
        var gate = new ProviderCallGate(cache, quota);
        var calls = 0;

        string? first = await gate.GetOrFetchAsync<string>("k", Ttl, 1, _ => { calls++; return Task.FromResult<string?>("value"); }, default);
        string? second = await gate.GetOrFetchAsync<string>("k", Ttl, 1, _ => { calls++; return Task.FromResult<string?>("value"); }, default);

        Assert.Equal("value", first);
        Assert.Equal("value", second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Does_not_spend_quota_on_a_cache_hit()
    {
        var cache = new InMemoryCacheStore();
        var quota = new CountingQuotaGuard(allow: true);
        var gate = new ProviderCallGate(cache, quota);

        await gate.GetOrFetchAsync<string>("k", Ttl, 1, _ => Task.FromResult<string?>("value"), default);
        await gate.GetOrFetchAsync<string>("k", Ttl, 1, _ => Task.FromResult<string?>("value"), default);

        Assert.Equal(1, quota.Consumed);
    }

    [Fact]
    public async Task Serves_cached_values_after_the_quota_is_spent()
    {
        var cache = new InMemoryCacheStore();
        var quota = new CountingQuotaGuard(allow: true);
        var gate = new ProviderCallGate(cache, quota);
        await gate.GetOrFetchAsync<string>("k", Ttl, 1, _ => Task.FromResult<string?>("cached"), default);

        var exhausted = new ProviderCallGate(cache, new CountingQuotaGuard(allow: false));

        Assert.Equal("cached", await exhausted.GetOrFetchAsync<string>("k", Ttl, 1, _ => Task.FromResult<string?>("fresh"), default));
    }

    [Fact]
    public async Task Throws_when_the_quota_is_spent_and_nothing_is_cached()
    {
        var gate = new ProviderCallGate(new InMemoryCacheStore(), new CountingQuotaGuard(allow: false));

        await Assert.ThrowsAsync<ProviderQuotaExhaustedException>(
            () => gate.GetOrFetchAsync<string>("k", Ttl, 1, _ => Task.FromResult<string?>("fresh"), default));
    }

    [Fact]
    public async Task Does_not_cache_a_null_result()
    {
        var cache = new InMemoryCacheStore();
        var gate = new ProviderCallGate(cache, new CountingQuotaGuard(allow: true));
        var calls = 0;

        await gate.GetOrFetchAsync<string>("k", Ttl, 1, _ => { calls++; return Task.FromResult<string?>(null); }, default);
        await gate.GetOrFetchAsync<string>("k", Ttl, 1, _ => { calls++; return Task.FromResult<string?>(null); }, default);

        Assert.Equal(2, calls);
    }

    private sealed class CountingQuotaGuard(bool allow) : IProviderQuotaGuard
    {
        public int Consumed { get; private set; }

        public Task<bool> TryConsumeAsync(int credits, CancellationToken ct)
        {
            Consumed += credits;
            return Task.FromResult(allow);
        }
    }
}
