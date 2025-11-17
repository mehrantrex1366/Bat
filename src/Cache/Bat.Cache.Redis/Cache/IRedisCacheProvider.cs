namespace Bat.Cache.Redis;

public interface IRedisCacheProvider : ISingletonInjection
{
    public IDatabase _redisDb { get; set; }
    public ConnectionMultiplexer _redisServer { get; set; }

    IServer GetServer(string host = null, int port = 0);
    IEnumerable<string> GetAllKey(RedisValue[] command, CommandFlags flags = CommandFlags.None);
    IEnumerable<string> GetAllKey(PagingParameter pagingParameter, string pattern = null, CommandFlags flags = CommandFlags.None);

    bool Set(string key, string value, TimeSpan? expiry = null, bool keepTTL = false);
    bool Set(string key, object value, TimeSpan? expiry = null, bool keepTTl = false);
    bool Set(KeyValuePair<string, string>[] values);
    bool Set(KeyValuePair<string, object>[] values, CommandFlags flags = CommandFlags.None);
    Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null, bool keepTTL = false);
    Task<bool> SetAsync(string key, object value, TimeSpan? expiry = null, bool keepTtl = false);
    Task<bool> SetAsync(KeyValuePair<string, string>[] values);
    Task<bool> SetAsync(KeyValuePair<string, object>[] values);

    string SetAndGet(string key, string value, TimeSpan? expiry = null, bool keepTTl = false);
    string SetAndGet(string key, object value, TimeSpan? expiry = null, bool keepTTl = false);
    Task<string> SetAndGetAsync(string key, string value, TimeSpan? expiry = null, bool keepTTl = false);
    Task<string> SetAndGetAsync(string key, object value, TimeSpan? expiry = null, bool keepTTl = false);
    TValue SetAndGet<TValue>(string key, string value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class;
    TValue SetAndGet<TValue>(string key, object value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class;
    Task<TValue> SetAndGetAsync<TValue>(string key, string value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class;
    Task<TValue> SetAndGetAsync<TValue>(string key, object value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class;

    T Get<T>(string key) where T : class;
    string Get(string key);
    string[] Get(string[] keys);
    Task<T> GetAsync<T>(string key);
    Task<string> GetAsync(string key);
    Task<string[]> GetAsync(string[] keys);

    string GetAndSet(string key, string value);
    string GetAndSet(string key, object value);
    Task<string> GetAndSetAsync(string key, string value);
    Task<string> GetAndSetAsync(string key, object value);
    TValue GetAndSet<TValue>(string key, string value) where TValue : class;
    TValue GetAndSet<TValue>(string key, object value) where TValue : class;
    Task<TValue> GetAndSetAsync<TValue>(string key, string value) where TValue : class;
    Task<TValue> GetAndSetAsync<TValue>(string key, object value) where TValue : class;

    bool Delete(string key);
    Task<bool> DeleteAsync(string key);
    void Rename(string oldKey, string newKey);
    Task RenameAsync(string oldKey, string newKey);
    bool Exists(string Key);
    Task<bool> ExistsAsync(string Key);
    long Length(string Key);
    Task<long> LengthAsync(string Key);
    bool Expire(string Key, TimeSpan? expiry = null);
    Task<bool> ExpireAsync(string Key, TimeSpan? expiry = null);

    bool Persist(string Key);
    Task<bool> PersistAsync(string Key);
    TimeSpan? Idle(string Key);
    Task<TimeSpan?> IdleAsync(string Key);
    long Increment(string Key);
    Task<long> IncrementAsync(string Key);
    long Decrement(string Key);
    Task<long> DecrementAsync(string Key);
}