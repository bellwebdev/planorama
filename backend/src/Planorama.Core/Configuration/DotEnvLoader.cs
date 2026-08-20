namespace Planorama.Core.Configuration;

/// <summary>
/// Derives local (non-Docker) dev config from the repo-root .env file — the same
/// gitignored source of truth Docker Compose interpolates into containers — so no
/// secret ever needs to be duplicated into a committed appsettings file.
/// Only intended to run under the Development environment; no-ops (and never
/// overwrites an already-set variable) otherwise, so Docker/CI/production are unaffected.
/// </summary>
public static class DotEnvLoader
{
    /// <summary>Local Postgres port mapped by docker-compose.yml (5432 is taken locally on this machine).</summary>
    private const int LocalDbPort = 5434;

    /// <summary>Local Redis port mapped by docker-compose.yml, offset for the same reason as <see cref="LocalDbPort"/>.</summary>
    private const int LocalCachePort = 6380;

    public static void ApplyLocalDevDefaults(string startDirectory)
    {
        var envFile = FindUpward(startDirectory, ".env");
        if (envFile is null)
        {
            return;
        }

        var values = Parse(envFile);

        if (Environment.GetEnvironmentVariable("ConnectionStrings__Db") is null
            && values.TryGetValue("POSTGRES_DB", out var db)
            && values.TryGetValue("POSTGRES_USER", out var user)
            && values.TryGetValue("POSTGRES_PASSWORD", out var password)
            && !string.IsNullOrEmpty(password))
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__Db",
                $"Host=localhost;Port={LocalDbPort};Database={db};Username={user};Password={password}");
        }

        Apply(values, "JWT_SIGNING_KEY", "Jwt__SigningKey");
        Apply(values, "GOOGLE_OAUTH_CLIENT_ID", "Google__ClientId");
        Apply(values, "GEOAPIFY_API_KEY", "Geoapify__ApiKey");

        // Docker publishes the cache on a non-default host port; without this a locally-run API
        // would try 6379 and silently run uncached.
        if (Environment.GetEnvironmentVariable("Redis__ConnectionString") is null)
        {
            Environment.SetEnvironmentVariable("Redis__ConnectionString", $"localhost:{LocalCachePort}");
        }
    }

    /// <summary>Copies one .env entry into its configuration-shaped environment variable, leaving
    /// an already-set variable alone so Docker and CI always win over the local file.</summary>
    private static void Apply(Dictionary<string, string> values, string envKey, string configKey)
    {
        if (Environment.GetEnvironmentVariable(configKey) is null
            && values.TryGetValue(envKey, out var value)
            && !string.IsNullOrEmpty(value))
        {
            Environment.SetEnvironmentVariable(configKey, value);
        }
    }

    private static Dictionary<string, string> Parse(string envFile)
    {
        var values = new Dictionary<string, string>();

        foreach (var line in File.ReadAllLines(envFile))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');
            values[key] = value;
        }

        return values;
    }

    private static string? FindUpward(string startDirectory, string fileName)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
