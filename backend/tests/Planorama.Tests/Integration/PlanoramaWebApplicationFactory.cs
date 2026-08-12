using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Planorama.Core.Data;
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
                ["Cors:AllowedOrigins:0"] = "https://app.test",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBackgroundJobClient>();
            services.AddScoped<IBackgroundJobClient, NoOpBackgroundJobClient>();
        });
    }

    async Task IAsyncLifetime.DisposeAsync() => await _postgres.DisposeAsync().AsTask();
}
