namespace Planorama.Api.Auth;

public record GoogleIdTokenPayload(string Subject, string Email, bool EmailVerified, string? Name, string? Picture);

public interface IGoogleIdTokenValidator
{
    /// <exception cref="Planorama.Core.Exceptions.InvalidExternalTokenException">The token failed signature/issuer/audience/expiry verification.</exception>
    Task<GoogleIdTokenPayload> ValidateAsync(string idToken, CancellationToken ct);
}
