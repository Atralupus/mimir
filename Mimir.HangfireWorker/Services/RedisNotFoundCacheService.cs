using Microsoft.Extensions.Options;
using Mimir.Worker;
using Serilog;
using StackExchange.Redis;

namespace Mimir.HangfireWorker.Services;

public class RedisNotFoundCacheService : INotFoundCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly Configuration _configuration;
    private readonly ILogger _logger;

    public RedisNotFoundCacheService(
        IConnectionMultiplexer redis,
        IOptions<Configuration> configuration
    )
    {
        _redis = redis;
        _configuration = configuration.Value;
        _logger = Log.ForContext<RedisNotFoundCacheService>();
    }

    public async Task<bool> IsNotFoundCachedAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var exists = await db.KeyExistsAsync($"notfound:{key}");
            return exists;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking not found cache for key: {Key}", key);
            return false;
        }
    }

    public async Task CacheNotFoundAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var expiration = TimeSpan.FromDays(7);
            await db.StringSetAsync($"notfound:{key}", "1", expiration);
            _logger.Information(
                "Cached not found for key: {Key} with expiration: {ExpirationDays} days",
                key,
                7
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error caching not found for key: {Key}", key);
        }
    }

    public async Task RemoveFromCacheAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync($"notfound:{key}");
            _logger.Information("Removed from not found cache: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error removing from not found cache for key: {Key}", key);
        }
    }
}
