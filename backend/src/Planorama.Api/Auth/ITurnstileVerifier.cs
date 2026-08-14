namespace Planorama.Api.Auth;

public interface ITurnstileVerifier
{
    /// <exception cref="Planorama.Core.Exceptions.InvalidTurnstileTokenException">The token is missing, invalid, or failed Cloudflare's siteverify check.</exception>
    Task VerifyAsync(string? token, string? remoteIp, CancellationToken ct);
}
