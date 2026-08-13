using System.Text;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.S3;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Planorama.Api.Auth;
using Planorama.Api.Endpoints;
using Planorama.Api.ErrorHandling;
using Planorama.Api.Options;
using Planorama.Api.Storage;
using Planorama.Core.Auth;
using Planorama.Core.Configuration;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Media;
using Planorama.Core.Options;
using Planorama.Core.Profile;
using Planorama.Core.Settings;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Must run before CreateBuilder(args) snapshots environment variables into configuration.
if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
{
    DotEnvLoader.ApplyLocalDevDefaults(Directory.GetCurrentDirectory());
}

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddOptions<JwtOptions>()
        .BindConfiguration(JwtOptions.SectionName)
        .Validate(o => o.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters (set via Jwt__SigningKey)")
        .ValidateOnStart();
    builder.Services.AddOptions<CorsOptions>()
        .BindConfiguration(CorsOptions.SectionName);
    builder.Services.AddOptions<EmailOptions>()
        .BindConfiguration(EmailOptions.SectionName);
    builder.Services.AddOptions<GoogleOptions>()
        .BindConfiguration(GoogleOptions.SectionName);
    builder.Services.AddOptions<R2Options>()
        .BindConfiguration(R2Options.SectionName);

    builder.Services.AddDbContext<PlanoramaDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

    builder.Services
        .AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
            options.Password.RequiredLength = 10;
            options.Password.RequireNonAlphanumeric = true;
        })
        .AddEntityFrameworkStores<PlanoramaDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager();

    // Confirmation tokens use DataProtectorTokenProvider; without persisted keys they don't
    // survive a container restart, breaking confirmation links in prod (stateless containers).
    builder.Services.AddDataProtection()
        .PersistKeysToDbContext<PlanoramaDbContext>()
        .SetApplicationName("planorama");

    // Client-only: enqueues jobs into the same Postgres-backed queue Planorama.Worker polls.
    // No AddHangfireServer() here — the Api process never executes jobs, only Worker does.
    // Serializer settings must match Planorama.Worker's exactly, since both share one queue.
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Db"))));

    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
    builder.Services.AddScoped<IImageProcessor, SkiaImageProcessor>();
    builder.Services.AddScoped<IAvatarStorage, R2AvatarStorage>();
    builder.Services.AddScoped<IProfileService, ProfileService>();
    builder.Services.AddScoped<ISettingsService, SettingsService>();

    // First enum-backed DTO field to cross the API boundary — string names (not raw ints) for
    // every future one too, so this only needs deciding once.
    builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    // AWS SDK clients are documented thread-safe and expensive to construct — one shared instance.
    builder.Services.AddSingleton<IAmazonS3>(sp =>
    {
        var r2 = sp.GetRequiredService<IOptions<R2Options>>().Value;
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{r2.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            // AWSSDK.S3 v4's default trailing-checksum streaming mode
            // (STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER) isn't implemented by R2 — fall back to
            // the classic signing path, only computing/validating checksums when an operation
            // actually requires one.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        return new AmazonS3Client(r2.AccessKeyId, r2.SecretAccessKey, config);
    });

    // Defense-in-depth cap ~1MB above the app-level 5MB avatar check, so an oversized multipart
    // body is rejected by the form reader before much of it is buffered.
    builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 6 * 1024 * 1024);

    builder.Services
        .AddAuthentication()
        .AddJwtBearer();

    // Configured via IOptions<JwtOptions>, resolved lazily at host-build time rather than read
    // directly off builder.Configuration — WebApplicationFactory-based tests override config
    // after this point in Program.cs runs, so an eager read here would see stale/empty values.
    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
        {
            // Without this, the framework remaps the "sub" claim JwtAccessTokenIssuer sets to the
            // legacy ClaimTypes.NameIdentifier URI on the way in, breaking ClaimsPrincipalExtensions.GetUserId().
            bearerOptions.MapInboundClaims = false;

            var jwt = jwtOptions.Value;
            bearerOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = string.IsNullOrEmpty(jwt.SigningKey)
                    ? null
                    : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });
    builder.Services.AddAuthorization();

    var cors = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(cors.AllowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

    builder.Services.AddExceptionHandler<AuthProblemExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
    app.MapGet("/readyz", async (PlanoramaDbContext db, CancellationToken ct) =>
        await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { status = "ready" })
            : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "database unreachable"));

    var v1 = app.MapGroup("/api/v1");
    v1.MapGet("/meta", () => new ApiMeta("planorama", "v1"));
    v1.MapAuthEndpoints();
    v1.MapMeEndpoints();
    v1.MapMeSettingsEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) // dotnet-ef aborts the host by design
{
    Log.Fatal(ex, "API host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public record ApiMeta(string Name, string Version);

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
