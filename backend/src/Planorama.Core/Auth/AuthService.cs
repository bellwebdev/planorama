using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Exceptions;
using Planorama.Core.Jobs;
using Planorama.Core.Options;

namespace Planorama.Core.Auth;

/// <inheritdoc cref="IAuthService"/>
public class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    PlanoramaDbContext db,
    IAccessTokenIssuer accessTokenIssuer,
    IBackgroundJobClient backgroundJobClient,
    IOptions<JwtOptions> jwtOptions,
    IOptions<EmailOptions> emailOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly EmailOptions _email = emailOptions.Value;

    /// <inheritdoc/>
    public async Task<RegisterResult> RegisterAsync(string email, string password, string displayName, CancellationToken ct)
    {
        // Email is set here and never cleared, so every AppUser this service touches has a non-null
        // Email — the `!` uses below on `user.Email` rely on that invariant.
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var isDuplicate = createResult.Errors.Any(e =>
                e.Code == nameof(IdentityErrorDescriber.DuplicateEmail) ||
                e.Code == nameof(IdentityErrorDescriber.DuplicateUserName));

            if (isDuplicate)
            {
                throw new EmailAlreadyRegisteredException();
            }

            throw new RegistrationFailedException(string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }

        await EnqueueConfirmationEmailAsync(user);

        return new RegisterResult(user.Id, user.Email!);
    }

    /// <inheritdoc/>
    public async Task ConfirmEmailAsync(Guid userId, string token, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new InvalidTokenException();
        }

        if (user.EmailConfirmed)
        {
            return;
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            throw new InvalidTokenException();
        }
    }

    /// <inheritdoc/>
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (signInResult.IsNotAllowed)
        {
            throw new EmailNotConfirmedException();
        }

        if (signInResult.IsLockedOut)
        {
            throw new AccountLockedException();
        }

        if (!signInResult.Succeeded)
        {
            throw new InvalidCredentialsException();
        }

        var tokens = await IssueTokenPairAsync(user, Guid.NewGuid(), ct);
        return new LoginResult(user.Id, user.Email!, user.DisplayName, tokens); // Email is non-null — see RegisterAsync.
    }

    /// <inheritdoc/>
    public async Task<TokenPair> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = RefreshTokenHasher.Hash(refreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existing is null || existing.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new InvalidRefreshTokenException();
        }

        if (existing.RevokedAt is not null)
        {
            if (existing.RevocationReason == RefreshTokenRevocationReason.Rotated)
            {
                await RevokeFamilyAsync(existing.FamilyId, RefreshTokenRevocationReason.ReuseDetected, ct);
                logger.LogWarning(
                    "Refresh token reuse detected for user {UserId}, family {FamilyId} — entire token family revoked",
                    existing.UserId, existing.FamilyId);
            }

            throw new InvalidRefreshTokenException();
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString())
            ?? throw new InvalidRefreshTokenException();

        return await IssueTokenPairAsync(user, existing.FamilyId, ct, replacing: existing);
    }

    /// <inheritdoc/>
    public async Task ResendConfirmationAsync(string email, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.EmailConfirmed)
        {
            return;
        }

        await EnqueueConfirmationEmailAsync(user);
    }

    /// <inheritdoc/>
    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = RefreshTokenHasher.Hash(refreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null || existing.RevokedAt is not null)
        {
            return;
        }

        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.RevocationReason = RefreshTokenRevocationReason.UserLogout;
        await db.SaveChangesAsync(ct);
    }

    private async Task<TokenPair> IssueTokenPairAsync(AppUser user, Guid familyId, CancellationToken ct, RefreshToken? replacing = null)
    {
        var accessToken = accessTokenIssuer.Issue(user);

        var rawRefreshToken = RefreshTokenHasher.GenerateRaw();
        var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays);
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = RefreshTokenHasher.Hash(rawRefreshToken),
            ExpiresAt = refreshTokenExpiresAt,
        };

        db.RefreshTokens.Add(newRefreshToken);

        if (replacing is not null)
        {
            replacing.RevokedAt = DateTimeOffset.UtcNow;
            replacing.RevocationReason = RefreshTokenRevocationReason.Rotated;
            replacing.ReplacedByTokenId = newRefreshToken.Id;
        }

        await db.SaveChangesAsync(ct);

        return new TokenPair(accessToken.Value, accessToken.ExpiresAt, rawRefreshToken, refreshTokenExpiresAt);
    }

    private async Task RevokeFamilyAsync(Guid familyId, RefreshTokenRevocationReason reason, CancellationToken ct)
    {
        var activeTokens = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevocationReason = reason;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task EnqueueConfirmationEmailAsync(AppUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = $"{_email.ConfirmationUrlBase}?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        backgroundJobClient.Enqueue<IEmailDispatchJob>(j => j.SendEmailConfirmationAsync(user.Email!, user.DisplayName, confirmationUrl)); // Email is non-null — see RegisterAsync.
    }
}
