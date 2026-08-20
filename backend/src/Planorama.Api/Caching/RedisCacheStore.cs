using System.Text.Json;
using Planorama.Core.Caching;
using StackExchange.Redis;

namespace Planorama.Api.Caching;

/// <inheritdoc cref="ICacheStore"/>
public class RedisCacheStore(IConnectionMultiplexer redis, ILogger<RedisCacheStore> logger) : ICacheStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
        try
        {
            RedisValue value = await redis.GetDatabase().StringGetAsync(key);
            return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<T>(value!, SerializerOptions);
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            // A cache outage degrades to a provider call, never to a failed request.
            logger.LogWarning(ex, "Cache read failed for {CacheKey}", key);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class
    {
        try
        {
            await redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value, SerializerOptions), ttl);
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            logger.LogWarning(ex, "Cache write failed for {CacheKey}", key);
        }
    }

    /// <inheritdoc/>
    public async Task<long> IncrementAsync(string key, long by, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            IDatabase db = redis.GetDatabase();
            long total = await db.StringIncrementAsync(key, by);
            if (total == by)
            {
                await db.KeyExpireAsync(key, ttl);
            }

            return total;
        }
        catch (RedisException ex)
        {
            // Fail open: with the counter unreadable we can't prove the quota is spent, and the
            // free tier's own limit is the backstop. Blocking every call on a Redis blip would be
            // a self-inflicted outage for a far worse reason.
            logger.LogError(ex, "Quota counter unavailable for {CacheKey}; allowing the call", key);
            return 0;
        }
    }
}
