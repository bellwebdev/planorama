using Planorama.Core.Auth;
using Xunit;

namespace Planorama.Tests.Unit;

public class RefreshTokenHasherTests
{
    [Fact]
    public void GenerateRaw_returns_distinct_high_entropy_values()
    {
        var first = RefreshTokenHasher.GenerateRaw();
        var second = RefreshTokenHasher.GenerateRaw();

        Assert.NotEqual(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void Hash_is_deterministic_for_the_same_input()
    {
        var raw = RefreshTokenHasher.GenerateRaw();

        Assert.Equal(RefreshTokenHasher.Hash(raw), RefreshTokenHasher.Hash(raw));
    }

    [Fact]
    public void Hash_differs_for_different_inputs()
    {
        var a = RefreshTokenHasher.Hash(RefreshTokenHasher.GenerateRaw());
        var b = RefreshTokenHasher.Hash(RefreshTokenHasher.GenerateRaw());

        Assert.NotEqual(a, b);
    }
}
