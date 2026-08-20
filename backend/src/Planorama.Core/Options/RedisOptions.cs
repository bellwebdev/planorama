namespace Planorama.Core.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>StackExchange.Redis connection string, e.g. <c>cache:6379</c> on the compose network.</summary>
    public string ConnectionString { get; set; } = "localhost:6379";
}
