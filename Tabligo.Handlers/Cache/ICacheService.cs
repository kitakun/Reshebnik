namespace Tabligo.Handlers.Cache;

public interface ICacheService
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);
    ValueTask SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
}
