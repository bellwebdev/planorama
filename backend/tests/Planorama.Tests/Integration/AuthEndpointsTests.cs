using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planorama.Api.Contracts.Auth;
using Planorama.Core.Auth;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Xunit;

namespace Planorama.Tests.Integration;

public class AuthEndpointsTests(PlanoramaWebApplicationFactory factory) : IClassFixture<PlanoramaWebApplicationFactory>
{
    private const string ValidPassword = "Passw0rd!23";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Full_flow_register_confirm_login_refresh_logout()
    {
        var email = UniqueEmail();

        var registerResponse = await RegisterAsync(email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registered);

        var confirmResponse = await ConfirmEmailAsync(registered!.UserId);
        Assert.Equal(HttpStatusCode.NoContent, confirmResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, ValidPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.Equal(email, login!.User.Email);

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(login.Tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<TokenPairResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(login.Tokens.RefreshToken, rotated!.RefreshToken);

        var logoutResponse = await _client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(rotated.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshAfterLogout = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(rotated.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_409()
    {
        var email = UniqueEmail();
        var first = await RegisterAsync(email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await RegisterAsync(email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_with_weak_password_returns_400_validation_problem()
    {
        var response = await RegisterAsync(UniqueEmail(), "weak", "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_missing_idempotency_key_returns_400()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
        {
            Content = JsonContent.Create(new RegisterRequest(UniqueEmail(), ValidPassword, "Ada")),
        };
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_reused_idempotency_key_returns_409()
    {
        var key = Guid.NewGuid().ToString();

        var first = await RegisterAsync(UniqueEmail(), ValidPassword, "Ada", key);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await RegisterAsync(UniqueEmail(), ValidPassword, "Grace", key);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var email = UniqueEmail();
        await RegisterAndConfirmAsync(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPassword!23"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401_same_shape_as_wrong_password()
    {
        var unknownResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(UniqueEmail(), ValidPassword));

        var email = UniqueEmail();
        await RegisterAndConfirmAsync(email);
        var wrongPasswordResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPassword!23"));

        Assert.Equal(HttpStatusCode.Unauthorized, unknownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(await unknownResponse.Content.ReadAsStringAsync(), await wrongPasswordResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_before_email_confirmed_returns_403()
    {
        var email = UniqueEmail();
        var registerResponse = await RegisterAsync(email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, ValidPassword));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_email_with_invalid_token_returns_400()
    {
        var email = UniqueEmail();
        var registerResponse = await RegisterAsync(email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/confirm-email", new ConfirmEmailRequest(registered!.UserId, "not-a-real-token"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_with_expired_token_returns_401()
    {
        var email = UniqueEmail();
        var login = await RegisterConfirmAndLoginAsync(email);

        await ExpireRefreshTokenAsync(login.Tokens.RefreshToken);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(login.Tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_reusing_an_already_rotated_token_revokes_the_whole_family()
    {
        var email = UniqueEmail();
        var login = await RegisterConfirmAndLoginAsync(email);

        var rotateResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(login.Tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<TokenPairResponse>();

        // Replay the original (now-rotated) token — reuse detection should fire.
        var replay = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(login.Tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // The legitimate successor should now be revoked too — the whole family was killed.
        var successorAttempt = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(rotated!.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, successorAttempt.StatusCode);
    }

    [Fact]
    public async Task Resend_confirmation_for_unknown_email_returns_202_without_revealing_existence()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/resend-confirmation")
        {
            Content = JsonContent.Create(new ResendConfirmationRequest(UniqueEmail())),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private async Task<HttpResponseMessage> RegisterAsync(string email, string password, string displayName, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
        {
            Content = JsonContent.Create(new RegisterRequest(email, password, displayName)),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> ConfirmEmailAsync(Guid userId)
    {
        var token = await GenerateConfirmationTokenAsync(userId);
        return await _client.PostAsJsonAsync("/api/v1/auth/confirm-email", new ConfirmEmailRequest(userId, token));
    }

    private async Task<string> GenerateConfirmationTokenAsync(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        return await userManager.GenerateEmailConfirmationTokenAsync(user!);
    }

    private async Task RegisterAndConfirmAsync(string email)
    {
        var registerResponse = await RegisterAsync(email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        var confirmResponse = await ConfirmEmailAsync(registered!.UserId);
        Assert.Equal(HttpStatusCode.NoContent, confirmResponse.StatusCode);
    }

    private async Task<LoginResponse> RegisterConfirmAndLoginAsync(string email)
    {
        await RegisterAndConfirmAsync(email);
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, ValidPassword));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return login!;
    }

    private async Task ExpireRefreshTokenAsync(string rawRefreshToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlanoramaDbContext>();
        var hash = RefreshTokenHasher.Hash(rawRefreshToken);
        var token = await db.RefreshTokens.FirstAsync(t => t.TokenHash == hash);
        token.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
    }
}
