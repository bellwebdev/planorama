using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Planorama.Core.Configuration;

namespace Planorama.Core.Data;

/// <summary>
/// Used only by `dotnet ef` at design time; the connection string is never opened
/// unless a migration command actually needs the database. Credentials come from the
/// repo-root .env (via DotEnvLoader) — never hard-coded here.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlanoramaDbContext>
{
    public PlanoramaDbContext CreateDbContext(string[] args)
    {
        DotEnvLoader.ApplyLocalDevDefaults(Directory.GetCurrentDirectory());

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Db")
            ?? throw new InvalidOperationException(
                "Could not derive a local dev connection string. Set POSTGRES_DB / POSTGRES_USER / POSTGRES_PASSWORD in .env at the repo root.");

        var options = new DbContextOptionsBuilder<PlanoramaDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new PlanoramaDbContext(options);
    }
}
