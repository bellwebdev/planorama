namespace Planorama.Core.Auth;

public interface IAuthService
{
    /// <summary>
    /// Creates the account and enqueues the confirmation email as a separate, independently-failable
    /// step — the account is persisted even if email dispatch later fails, so a Resend/network issue
    /// never rolls back a registration. Use <see cref="ResendConfirmationAsync"/> to retry the email.
    /// </summary>
    /// <exception cref="Exceptions.EmailAlreadyRegisteredException">The email (or derived username) is already registered.</exception>
    Task<RegisterResult> RegisterAsync(string email, string password, string displayName, CancellationToken ct);

    /// <summary>Idempotent: confirming an already-confirmed account is a silent no-op rather than an error.</summary>
    /// <exception cref="Exceptions.InvalidTokenException">The user doesn't exist, or the token is invalid/expired.</exception>
    Task ConfirmEmailAsync(Guid userId, string token, CancellationToken ct);

    /// <summary>
    /// Unknown email and wrong password both throw <see cref="Exceptions.InvalidCredentialsException"/>
    /// with an identical message — this is deliberate enumeration-safety, not an oversight.
    /// </summary>
    /// <exception cref="Exceptions.InvalidCredentialsException">Unknown email or wrong password.</exception>
    /// <exception cref="Exceptions.EmailNotConfirmedException">Credentials are valid but the email isn't confirmed yet.</exception>
    /// <exception cref="Exceptions.AccountLockedException">Too many recent failed attempts (ASP.NET Identity's own lockout).</exception>
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct);

    /// <summary>
    /// Rotates the refresh token. Presenting a token that was already rotated away is treated as a
    /// stolen-token replay signal: the entire token family (every token issued since the original
    /// login) is revoked, not just the presented one.
    /// </summary>
    /// <exception cref="Exceptions.InvalidRefreshTokenException">Unknown, expired, or already-rotated/revoked token.</exception>
    Task<TokenPair> RefreshAsync(string refreshToken, CancellationToken ct);

    /// <summary>Silently no-ops for an unknown or already-confirmed email — deliberately enumeration-safe, unlike <see cref="RegisterAsync"/>.</summary>
    Task ResendConfirmationAsync(string email, CancellationToken ct);

    /// <summary>Idempotent: an unknown or already-revoked token is a silent no-op, since logout is inherently a "make sure it's gone" operation.</summary>
    Task LogoutAsync(string refreshToken, CancellationToken ct);
}
