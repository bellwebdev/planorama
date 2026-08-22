using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planorama.Core.Configuration;
using Planorama.Core.Data;
using Planorama.Core.Integrations;
using Planorama.Core.Itinerary;
using Planorama.Core.Jobs;
using Planorama.Core.Notifications;
using Planorama.Core.Suggestions;
using Planorama.Worker.Emails;
using Planorama.Worker.Jobs;
using Planorama.Core.Options;
using Planorama.Worker.Options;
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

    builder.Services.AddDbContext<PlanoramaDbContext>(options => options.UseNpgsql(connectionString));

    // Injected rather than DateTimeOffset.UtcNow, matching Planorama.Api — keeps resolution timing testable.
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<ICoinFlip, CryptoCoinFlip>();
    builder.Services.AddScoped<VoteResultNotifier>();
    builder.Services.AddScoped<ReminderScheduler>();
    builder.Services.AddScoped<ItinerarySyncService>();
    builder.Services.AddScoped<IVotingResolutionJob, VotingResolutionService>();

    builder.Services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.SectionName);
    builder.Services.AddOptions<SmtpOptions>().BindConfiguration(SmtpOptions.SectionName);

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddScoped<IEmailSender, LogOnlyEmailSender>();
    }
    else
    {
        builder.Services.AddScoped<IEmailSender, MailKitSmtpEmailSender>();
    }

    builder.Services.AddScoped<IEmailDispatchJob, EmailDispatchJob>();

    // Phase 2 jobs (reminders) register here.

    var host = builder.Build();

    // Every 5 minutes: closes voting windows and resolves ties. Self-healing — a missed tick just
    // catches up on the next run, so no per-suggestion scheduling is needed (spec §6.5-6.6).
    // Resolved via DI (IRecurringJobManager), not the static RecurringJob.AddOrUpdate — the static
    // API depends on JobStorage.Current, which AddHangfire's service registration does not set
    // synchronously and crashed the host on startup.
    using (var scope = host.Services.CreateScope())
    {
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobs.AddOrUpdate<IVotingResolutionJob>(
            "resolve-due-suggestions", j => j.ResolveDueSuggestionsAsync(), "*/5 * * * *");
    }

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
