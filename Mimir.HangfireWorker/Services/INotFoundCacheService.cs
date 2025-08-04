namespace Mimir.HangfireWorker.Services;

public interface INotFoundCacheService
{
    Task<bool> IsNotFoundCachedAsync(string key);
    Task CacheNotFoundAsync(string key);
    Task RemoveFromCacheAsync(string key);
} 