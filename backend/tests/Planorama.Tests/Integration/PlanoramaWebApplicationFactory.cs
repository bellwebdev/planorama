using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Planorama.Api.Auth;
using Planorama.Core.Caching;
using Planorama.Core.Data;
using Planorama.Core.Integrations;
using Planorama.Core.Media;
using Planorama.Core.Suggestions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Planorama.Tests.Integration;

/// <summary>
/// Shared integration-test harness: real Postgres via Testcontainers (not a fake/in-memory
/// provider, so migrations and constraints behave exactly as in production), with the real
/// Hangfire client swapped for <see cref="NoOpBackgroundJobClient"/> so tests don't depend on
/// Hangfire's own storage/timing. First harness in the repo — reusable by future features.
/// </summary>
public class PlanoramaWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlanoramaDbContext>();
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Db"] = _postgres.GetConnectionString(),
                ["Jwt:SigningKey"] = "test-signing-key-at-least-32-characters-long-000",
                ["Jwt:Issuer"] = "planorama-tests",
                ["Jwt:Audience"] = "planorama-tests",
                ["Email:ConfirmationUrlBase"] = "https://app.test/confirm-email",
                ["Email:SuggestionUrlBase"] = "https://app.test/trips",
                ["Cors:AllowedOrigins:0"] = "https://app.test",
                ["Google:ClientId"] = "test-client-id",
                // Satisfies the ValidateOnStart check; every outbound Geoapify call is faked below.
                ["Geoapify:ApiKey"] = "test-api-key",
                // Well above anything a shared test run could hit — these buckets exist to catch
                // real abuse, not to be exercised by the test suite's own repeated register calls.
                ["RateLimiting:AuthRegister:PermitLimit"] = "10000",
                ["RateLimiting:AuthRegister:WindowMinutes"] = "15",
                ["RateLimiting:AuthResendConfirmation:PermitLimit"] = "10000",
                ["RateLimiting:AuthResendConfirmation:WindowMinutes"] = "15",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Singleton, not scoped: tests resolve this same instance from the factory afterwards
            // to assert on NoOpBackgroundJobClient.EnqueuedJobs.
            services.RemoveAll<IBackgroundJobClient>();
            services.AddSingleton<IBackgroundJobClient, NoOpBackgroundJobClient>();

            services.RemoveAll<IGoogleIdTokenValidator>();
            services.AddScoped<IGoogleIdTokenValidator, FakeGoogleIdTokenValidator>();

            services.RemoveAll<ITurnstileVerifier>();
            services.AddScoped<ITurnstileVerifier, FakeTurnstileVerifier>();

            services.RemoveAll<IAvatarStorage>();
            services.AddScoped<IAvatarStorage, FakeAvatarStorage>();

            // Swapped at the interface the services consume, which is the cached decorator — the
            // caching and quota rules are unit-tested against ProviderCallGate instead, so these
            // tests stay about authorisation, binding and mapping.
            services.RemoveAll<IPlacesProvider>();
            services.AddScoped<IPlacesProvider, FakePlacesProvider>();

            services.RemoveAll<IRoutingProvider>();
            services.AddScoped<IRoutingProvider, FakeRoutingProvider>();

            // Singleton, not scoped: tests resolve this same instance from the factory afterwards
            // to assert on FakeGeocodingProvider.ReceivedAddresses.
            services.RemoveAll<IGeocodingProvider>();
            services.AddSingleton<IGeocodingProvider, FakeGeocodingProvider>();

            services.RemoveAll<ICacheStore>();
            services.AddSingleton<ICacheStore, InMemoryCacheStore>();

            // Singleton, not scoped: tests resolve this same instance from the factory afterwards
            // to set FakeCoinFlip.NextResult before triggering resolution.
            services.RemoveAll<ICoinFlip>();
            services.AddSingleton<ICoinFlip, FakeCoinFlip>();
        });
    }

    async Task IAsyncLifetime.DisposeAsync() => await _postgres.DisposeAsync().AsTask();
}
