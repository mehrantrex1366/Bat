namespace Bat.Cache;

public class MemoryCacheProvider : IMemoryCacheProvider
{
    private readonly MemoryCache _cache;
    public readonly string _cacheName = "Bat.MemoryCache";

    public MemoryCacheProvider()
    {
        _cache = new MemoryCache(_cacheName);
    }

    public MemoryCacheProvider(string cacheName)
    {
        _cacheName = cacheName;
        _cache = new MemoryCache(cacheName);
    }


    // Set overwrites an existing entry. It used MemoryCache.Add, which silently keeps the OLD value when the key
    // already exists, so updated data was never visible until the old entry expired. Use Add(...) for add-if-absent.
    public bool Set(string key, object value, DateTimeOffset expireTime) { _cache.Set(key, value, expireTime); return true; }

    public bool Add(string key, object value, CacheItemPolicy cachePolicy) => _cache.Add(key, value, cachePolicy);

    // Note: System.Runtime.Caching does not allow both absolute and sliding expiration on one item
    // (it throws ArgumentException); pass ObjectCache.InfiniteAbsoluteExpiration to use sliding expiration only.
    public bool Set(string key, object value, DateTimeOffset expireTime, TimeSpan slidingTime) { _cache.Set(key, value, new CacheItemPolicy { AbsoluteExpiration = expireTime, SlidingExpiration = slidingTime }); return true; }

    public object GetSet(string key, object value, DateTimeOffset expireTime) => _cache.AddOrGetExisting(key, value, expireTime);

    public object AddOrGetExisting(string key, object value, CacheItemPolicy cachePolicy) => _cache.AddOrGetExisting(key, value, cachePolicy);

    public bool Delete(string key) { var cacheItem = _cache.Remove(key); return cacheItem != null; }

    public object Get(string key) => _cache.Get(key);

    public CacheItem GetCacheItem(string key) => _cache.GetCacheItem(key);

    public IDictionary<string, object> GetAll(IEnumerable<string> keys) => _cache.GetValues(keys);

    public long GetCount() => _cache.GetCount();

    public long GetSize() => _cache.GetLastSize();

    public long GetMemoryLimit() => _cache.PhysicalMemoryLimit;

    public object TrimMemory(int percentage) => _cache.Trim(percentage);
}