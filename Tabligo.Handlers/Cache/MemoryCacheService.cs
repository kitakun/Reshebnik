using Microsoft.Extensions.Caching.Memory;

namespace Tabligo.Handlers.Cache;

public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return new(cache.TryGetValue(key, out var value) ? (T?)value : default);
    }

    public ValueTask SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        if (value is null)
            return ValueTask.CompletedTask;

        var options = new MemoryCacheEntryOptions();
        if (ttl.HasValue)
            options.AbsoluteExpirationRelativeToNow = ttl;
        cache.Set(key, value, options);
        return ValueTask.CompletedTask;
    }
}
