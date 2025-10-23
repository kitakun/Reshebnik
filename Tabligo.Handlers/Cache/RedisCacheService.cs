namespace Tabligo.Handlers.Cache;

// TODO: implement Redis caching
public class RedisCacheService : ICacheService
{
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return ValueTask.FromResult<T?>(default);
    }

    public ValueTask SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }
}
