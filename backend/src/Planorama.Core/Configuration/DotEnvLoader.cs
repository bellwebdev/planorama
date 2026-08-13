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

        if (Environment.GetEnvironmentVariable("Jwt__SigningKey") is null
            && values.TryGetValue("JWT_SIGNING_KEY", out var signingKey)
            && !string.IsNullOrEmpty(signingKey))
        {
            Environment.SetEnvironmentVariable("Jwt__SigningKey", signingKey);
        }

        if (Environment.GetEnvironmentVariable("Google__ClientId") is null
            && values.TryGetValue("GOOGLE_OAUTH_CLIENT_ID", out var googleClientId)
            && !string.IsNullOrEmpty(googleClientId))
        {
            Environment.SetEnvironmentVariable("Google__ClientId", googleClientId);
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
