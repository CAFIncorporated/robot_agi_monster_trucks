using Microsoft.Extensions.Caching.Memory;

namespace CoordinateService.Services;

public interface ICacheService
{
    T? Get<T>(string key) where T : class;
    void Set<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    void Evict(string key);
    void EvictByPrefix(string prefix);
}

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly HashSet<string> _keys = [];
    private readonly Lock _lock = new();
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);

    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public T? Get<T>(string key) where T : class => _cache.TryGetValue(key, out T? val) ? val : null;

    public void Set<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        _cache.Set(key, value, expiry ?? DefaultExpiry);
        lock (_lock) { _keys.Add(key); }
    }

    public void Evict(string key)
    {
        _cache.Remove(key);
        lock (_lock) { _keys.Remove(key); }
    }

    public void EvictByPrefix(string prefix)
    {
        List<string> toRemove;
        lock (_lock) { toRemove = _keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList(); }
        foreach (var key in toRemove) Evict(key);
    }
}
