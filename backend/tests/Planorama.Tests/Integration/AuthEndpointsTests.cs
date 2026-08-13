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

[Collection("Api")]
public class AuthEndpointsTests(PlanoramaWebApplicationFactory factory)
{
    private const string ValidPassword = AuthTestHelpers.ValidPassword;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Full_flow_register_confirm_login_refresh_logout()
    {
        var email = AuthTestHelpers.UniqueEmail();

        var registerResponse = await AuthTestHelpers.RegisterAsync(_client, email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registered);

        var confirmResponse = await AuthTestHelpers.ConfirmEmailAsync(_client, factory, registered!.UserId);
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
        var email = AuthTestHelpers.UniqueEmail();
        var first = await AuthTestHelpers.RegisterAsync(_client, email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await AuthTestHelpers.RegisterAsync(_client, email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_with_weak_password_returns_400_validation_problem()
    {
        var response = await AuthTestHelpers.RegisterAsync(_client, AuthTestHelpers.UniqueEmail(), "weak", "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_missing_idempotency_key_returns_400()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
        {
            Content = JsonContent.Create(new RegisterRequest(AuthTestHelpers.UniqueEmail(), ValidPassword, "Ada")),
        };
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_reused_idempotency_key_returns_409()
    {
        var key = Guid.NewGuid().ToString();

        var first = await AuthTestHelpers.RegisterAsync(_client, AuthTestHelpers.UniqueEmail(), ValidPassword, "Ada", key);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await AuthTestHelpers.RegisterAsync(_client, AuthTestHelpers.UniqueEmail(), ValidPassword, "Grace", key);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var email = AuthTestHelpers.UniqueEmail();
        await AuthTestHelpers.RegisterAndConfirmAsync(_client, factory, email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPassword!23"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401_same_shape_as_wrong_password()
    {
        var unknownResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(AuthTestHelpers.UniqueEmail(), ValidPassword));

        var email = AuthTestHelpers.UniqueEmail();
        await AuthTestHelpers.RegisterAndConfirmAsync(_client, factory, email);
        var wrongPasswordResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPassword!23"));

        Assert.Equal(HttpStatusCode.Unauthorized, unknownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(await unknownResponse.Content.ReadAsStringAsync(), await wrongPasswordResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_before_email_confirmed_returns_403()
    {
        var email = AuthTestHelpers.UniqueEmail();
        var registerResponse = await AuthTestHelpers.RegisterAsync(_client, email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, ValidPassword));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_email_with_invalid_token_returns_400()
    {
        var email = AuthTestHelpers.UniqueEmail();
        var registerResponse = await AuthTestHelpers.RegisterAsync(_client, email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/confirm-email", new ConfirmEmailRequest(registered!.UserId, "not-a-real-token"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_with_expired_token_returns_401()
    {
        var email = AuthTestHelpers.UniqueEmail();
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, email);

        await ExpireRefreshTokenAsync(login.Tokens.RefreshToken);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(login.Tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_reusing_an_already_rotated_token_revokes_the_whole_family()
    {
        var email = AuthTestHelpers.UniqueEmail();
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, email);

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
            Content = JsonContent.Create(new ResendConfirmationRequest(AuthTestHelpers.UniqueEmail())),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Google_signin_with_new_email_creates_confirmed_account()
    {
        var email = AuthTestHelpers.UniqueEmail();
        var response = await GoogleSignInAsync($"{Guid.NewGuid():N}|{email}|true|Ada Lovelace");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.Equal(email, login!.User.Email);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(login.User.Id.ToString());
        Assert.True(user!.EmailConfirmed);
    }

    [Fact]
    public async Task Google_signin_twice_with_same_subject_returns_same_user_id()
    {
        var token = $"{Guid.NewGuid():N}|{AuthTestHelpers.UniqueEmail()}|true|Ada Lovelace";

        var first = await GoogleSignInAsync(token);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstLogin = await first.Content.ReadFromJsonAsync<LoginResponse>();

        var second = await GoogleSignInAsync(token);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondLogin = await second.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(firstLogin!.User.Id, secondLogin!.User.Id);
        Assert.NotEqual(firstLogin.Tokens.RefreshToken, secondLogin.Tokens.RefreshToken);
    }

    [Fact]
    public async Task Google_signin_links_to_existing_password_account_with_matching_email()
    {
        var email = AuthTestHelpers.UniqueEmail();
        var passwordLogin = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, email);

        var googleResponse = await GoogleSignInAsync($"{Guid.NewGuid():N}|{email}|true|Ada Lovelace");
        Assert.Equal(HttpStatusCode.OK, googleResponse.StatusCode);
        var googleLogin = await googleResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(passwordLogin.User.Id, googleLogin!.User.Id);
    }

    [Fact]
    public async Task Google_signin_with_unverified_email_returns_403()
    {
        var response = await GoogleSignInAsync($"{Guid.NewGuid():N}|{AuthTestHelpers.UniqueEmail()}|false");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Google_signin_with_invalid_token_returns_401()
    {
        var response = await GoogleSignInAsync("invalid");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Google_signin_missing_id_token_returns_400_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/external/google", new GoogleSignInRequest(""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> GoogleSignInAsync(string fakeIdToken) =>
        _client.PostAsJsonAsync("/api/v1/auth/external/google", new GoogleSignInRequest(fakeIdToken));

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
