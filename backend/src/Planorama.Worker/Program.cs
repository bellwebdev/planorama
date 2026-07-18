using Hangfire;
using Hangfire.PostgreSql;
using Planorama.Core.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Must run before CreateApplicationBuilder(args) snapshots environment variables into configuration.
if (string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
{
    DotEnvLoader.ApplyLocalDevDefaults(Directory.GetCurrentDirectory());
}

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, config) => config
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    var connectionString = builder.Configuration.GetConnectionString("Db")
        ?? throw new InvalidOperationException("ConnectionStrings:Db is required");

    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

    builder.Services.AddHangfireServer();

    // Phase 2 jobs (vote-window resolution, reminders, email dispatch) register here.

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
