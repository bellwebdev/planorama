using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Planorama.Core.Data;

/// <summary>
/// Used only by `dotnet ef` at design time; the connection string is never opened
/// unless a migration command actually needs the database.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlanoramaDbContext>
{
    public PlanoramaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlanoramaDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=planorama;Username=planorama;Password=planorama")
            .Options;
        return new PlanoramaDbContext(options);
    }
}
