using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Planorama.Api.Options;
using Planorama.Core.Configuration;
using Planorama.Core.Data;
using Planorama.Core.Domain;
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

    builder.Services.AddDbContext<PlanoramaDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

    builder.Services
        .AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<PlanoramaDbContext>()
        .AddDefaultTokenProviders();

    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    builder.Services
        .AddAuthentication()
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
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
