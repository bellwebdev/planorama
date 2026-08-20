using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Api.Places;
using Planorama.Tests.Integration;
using Xunit;

namespace Planorama.Tests.Unit;

public class GeoapifyQuotaGuardTests
{
    [Fact]
    public async Task Allows_calls_up_to_the_soft_cap_then_refuses()
    {
        // 100 credits at a 50% soft cap leaves 50 spendable.
        GeoapifyQuotaGuard guard = CreateGuard(dailyCap: 100, softCapFraction: 0.5);

        for (var spent = 0; spent < 50; spent++)
        {
            Assert.True(await guard.TryConsumeAsync(1, default));
        }

        Assert.False(await guard.TryConsumeAsync(1, default));
    }

    [Fact]
    public async Task Counts_multi_credit_calls_in_full()
    {
        GeoapifyQuotaGuard guard = CreateGuard(dailyCap: 100, softCapFraction: 0.5);

        Assert.True(await guard.TryConsumeAsync(50, default));
        Assert.False(await guard.TryConsumeAsync(1, default));
    }

    private static GeoapifyQuotaGuard CreateGuard(int dailyCap, double softCapFraction) =>
        new(new InMemoryCacheStore(),
            Options.Create(new GeoapifyOptions { ApiKey = "k", DailyCreditCap = dailyCap, SoftCapFraction = softCapFraction }),
            NullLogger<GeoapifyQuotaGuard>.Instance);
}
