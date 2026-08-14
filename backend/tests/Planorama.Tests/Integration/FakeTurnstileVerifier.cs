using Planorama.Api.Auth;
using Planorama.Core.Exceptions;

namespace Planorama.Tests.Integration;

/// <summary>Test double standing in for the real Cloudflare siteverify call. Accepts anything
/// except the sentinel "invalid", which simulates a failed challenge.</summary>
public class FakeTurnstileVerifier : ITurnstileVerifier
{
    public Task VerifyAsync(string? token, string? remoteIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token == "invalid")
        {
            throw new InvalidTurnstileTokenException();
        }

        return Task.CompletedTask;
    }
}
