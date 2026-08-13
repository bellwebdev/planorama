using Planorama.Api.Auth;
using Planorama.Api.Contracts.Auth;
using Planorama.Api.Filters;
using Planorama.Core.Auth;

namespace Planorama.Api.Endpoints;

public static class AuthEndpoints
{
    /// <summary>Maps the seven <c>/auth/*</c> routes. All are anonymous by design — this is the credential bootstrap surface itself.</summary>
    public static void MapAuthEndpoints(this RouteGroupBuilder v1)
    {
        var auth = v1.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/register", async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
            {
                var result = await authService.RegisterAsync(request.Email, request.Password, request.DisplayName, ct);
                return Results.Created($"/api/v1/users/{result.UserId}", new RegisterResponse(result.UserId, result.Email));
            })
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .AddEndpointFilter(new IdempotencyFilter("auth/register"));

        auth.MapPost("/confirm-email", async (ConfirmEmailRequest request, IAuthService authService, CancellationToken ct) =>
            {
                await authService.ConfirmEmailAsync(request.UserId, request.Token, ct);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<ConfirmEmailRequest>>();

        auth.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
            {
                var result = await authService.LoginAsync(request.Email, request.Password, ct);
                return Results.Ok(new LoginResponse(
                    new UserSummary(result.UserId, result.Email, result.DisplayName),
                    MapTokens(result.Tokens)));
            })
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        auth.MapPost("/refresh", async (RefreshRequest request, IAuthService authService, CancellationToken ct) =>
            {
                var tokens = await authService.RefreshAsync(request.RefreshToken, ct);
                return Results.Ok(MapTokens(tokens));
            })
            .AddEndpointFilter<ValidationFilter<RefreshRequest>>();

        auth.MapPost("/logout", async (LogoutRequest request, IAuthService authService, CancellationToken ct) =>
            {
                await authService.LogoutAsync(request.RefreshToken, ct);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<LogoutRequest>>();

        auth.MapPost("/resend-confirmation", async (ResendConfirmationRequest request, IAuthService authService, CancellationToken ct) =>
            {
                await authService.ResendConfirmationAsync(request.Email, ct);
                return Results.Accepted();
            })
            .AddEndpointFilter<ValidationFilter<ResendConfirmationRequest>>()
            .AddEndpointFilter(new IdempotencyFilter("auth/resend-confirmation"));

        auth.MapPost("/external/google", async (GoogleSignInRequest request, IGoogleIdTokenValidator validator, IAuthService authService, CancellationToken ct) =>
            {
                var payload = await validator.ValidateAsync(request.IdToken, ct);
                var identity = new ExternalLoginIdentity("Google", payload.Subject, payload.Email, payload.EmailVerified, payload.Name, payload.Picture);
                var result = await authService.ExternalLoginAsync(identity, ct);
                return Results.Ok(new LoginResponse(
                    new UserSummary(result.UserId, result.Email, result.DisplayName),
                    MapTokens(result.Tokens)));
            })
            .AddEndpointFilter<ValidationFilter<GoogleSignInRequest>>();
    }

    private static TokenPairResponse MapTokens(TokenPair tokens) =>
        new(tokens.AccessToken, tokens.AccessTokenExpiresAt, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
}
