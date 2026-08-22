using Planorama.Core.Suggestions;

namespace Planorama.Tests.Integration;

/// <summary>Test double for the tie-break coin flip — deterministic instead of crypto-random so
/// tests can assert on both outcomes. Singleton, like <see cref="FakeGeocodingProvider"/>: tests
/// resolve this same instance from the factory to set <see cref="NextResult"/> before triggering
/// resolution.</summary>
public class FakeCoinFlip : ICoinFlip
{
    public bool NextResult { get; set; } = true;

    public bool FlipApproved() => NextResult;
}
