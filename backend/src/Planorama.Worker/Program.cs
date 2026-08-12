using Hangfire;
using Hangfire.PostgreSql;
using Planorama.Core.Configuration;
using Planorama.Core.Integrations;
using Planorama.Core.Jobs;
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

    builder.Services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.SectionName);
    builder.Services.AddOptions<ResendOptions>().BindConfiguration(ResendOptions.SectionName);

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddScoped<IEmailSender, LogOnlyEmailSender>();
    }
    else
    {
        var resendOptions = builder.Configuration.GetSection(ResendOptions.SectionName).Get<ResendOptions>() ?? new ResendOptions();
        Resend.ResendDiExtensions.AddResend(builder.Services, options => options.ApiToken = resendOptions.ApiKey);
        builder.Services.AddScoped<IEmailSender, ResendEmailSender>();
    }

    builder.Services.AddScoped<IEmailDispatchJob, EmailDispatchJob>();

    // Phase 2 jobs (vote-window resolution, reminders) register here.

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
