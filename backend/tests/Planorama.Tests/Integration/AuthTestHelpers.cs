using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Planorama.Api.Contracts.Auth;
using Planorama.Core.Domain;

namespace Planorama.Tests.Integration;

/// <summary>Shared register/confirm/login flow used by both <see cref="AuthEndpointsTests"/> and <see cref="MeEndpointsTests"/> to get an authenticated user + access token.</summary>
public static class AuthTestHelpers
{
    public const string ValidPassword = "Passw0rd!23";

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string password, string displayName, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
        {
            Content = JsonContent.Create(new RegisterRequest(email, password, displayName)),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    public static async Task<string> GenerateConfirmationTokenAsync(PlanoramaWebApplicationFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        return await userManager.GenerateEmailConfirmationTokenAsync(user!);
    }

    public static async Task<HttpResponseMessage> ConfirmEmailAsync(HttpClient client, PlanoramaWebApplicationFactory factory, Guid userId)
    {
        var token = await GenerateConfirmationTokenAsync(factory, userId);
        return await client.PostAsJsonAsync("/api/v1/auth/confirm-email", new ConfirmEmailRequest(userId, token));
    }

    public static async Task RegisterAndConfirmAsync(HttpClient client, PlanoramaWebApplicationFactory factory, string email)
    {
        var registerResponse = await RegisterAsync(client, email, ValidPassword, "Ada", Guid.NewGuid().ToString());
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        var confirmResponse = await ConfirmEmailAsync(client, factory, registered!.UserId);
        confirmResponse.EnsureSuccessStatusCode();
    }

    public static async Task<LoginResponse> RegisterConfirmAndLoginAsync(HttpClient client, PlanoramaWebApplicationFactory factory, string email)
    {
        await RegisterAndConfirmAsync(client, factory, email);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, ValidPassword));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return login!;
    }
}
