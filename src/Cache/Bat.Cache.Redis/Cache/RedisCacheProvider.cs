namespace Bat.Cache.Redis;

public class RedisCacheProvider : IRedisCacheProvider
{
    public IDatabase _redisCache { get; set; }
    private readonly RedisSettings _redisSettings;
    public ConnectionMultiplexer _redisServer { get; set; }

    public RedisCacheProvider(IOptions<RedisSettings> redisSettings)
    {
        _redisSettings = redisSettings.Value;
        if (_redisSettings.Server1.IsNullOrWhiteSpace() &&
            _redisSettings.Server2.IsNullOrWhiteSpace() &&
            _redisSettings.Server3.IsNullOrWhiteSpace())
            throw new Exception("redisSettings not configured properly in appSettings !");

        ConfigurationOptions config = new();
        if (_redisSettings.Server1.IsNullOrWhiteSpace() is false)
            config.EndPoints.Add(_redisSettings.Server1, _redisSettings.Port1);
        if (_redisSettings.Server2.IsNullOrWhiteSpace() is false)
            config.EndPoints.Add(_redisSettings.Server2, _redisSettings.Port2);
        if (_redisSettings.Server3.IsNullOrWhiteSpace() is false)
            config.EndPoints.Add(_redisSettings.Server3, _redisSettings.Port3);


        config.AllowAdmin = _redisSettings.AllowAdminCommand;
        config.User = _redisSettings.Username ?? null;
        config.Password = _redisSettings.Password ?? null;
        config.ConnectRetry = _redisSettings.ConnectRetry;
        config.SyncTimeout = _redisSettings.SyncTimeout;
        config.ConnectTimeout = _redisSettings.ConnectTimeout;
        config.ClientName = _redisSettings.ClientName ?? null;
        config.AbortOnConnectFail = _redisSettings.AbortOnConnectFail;
        config.IncludeDetailInExceptions = _redisSettings.IncludeDetailInExceptions;
        config.CheckCertificateRevocation = _redisSettings.CheckCertificateRevocation;
        if (_redisSettings.DefaultDatabaseIndex > 0) config.DefaultDatabase = _redisSettings.DefaultDatabaseIndex;

        // SslSettings is optional; it used to throw NullReferenceException when it was missing from appsettings.
        config.Ssl = _redisSettings.SslSettings?.UseSsl ?? false;
        config.SslHost = _redisSettings.SslSettings?.SslHost;
        if (_redisSettings.SslSettings is not null) config.SslProtocols = _redisSettings.SslSettings.Protocol;

        if (_redisSettings.IsSentinelConnect)
            _redisServer = ConnectionMultiplexer.SentinelConnect(config);
        else
            _redisServer = ConnectionMultiplexer.Connect(config);
        _redisCache = _redisServer.GetDatabase();
    }

    public RedisCacheProvider(ConnectionMultiplexer redisClient, IDatabase database = null)
    {
        _redisServer = redisClient;
        if (database is null)
            _redisCache = _redisServer.GetDatabase();
        else
            _redisCache = database;
    }

    public RedisCacheProvider(ConfigurationOptions configurationOptions, IDatabase database = null)
    {
        _redisServer = ConnectionMultiplexer.Connect(configurationOptions);
        if (database is null)
            _redisCache = _redisServer.GetDatabase();
        else
            _redisCache = database;
    }


    // Fixed: the port condition was inverted (a given port was ignored and port 0 was used otherwise),
    // and _redisSettings is null when the provider is built from a ConnectionMultiplexer/ConfigurationOptions.
    // JSON is written/read as UTF-8 bytes directly: the stored bytes are identical to the old
    // "serialize to string, then let Redis UTF-8 encode it" path, without the intermediate string.
    private static RedisValue ToRedisValue(object value) => value.SerializeToJsonUtf8Bytes();

    private static T FromRedisValue<T>(RedisValue value) => ((byte[])value).DeSerializeJson<T>();

    private static KeyValuePair<RedisKey, RedisValue>[] ToRedisPairs(KeyValuePair<string, string>[] values)
    {
        var list = new KeyValuePair<RedisKey, RedisValue>[values.Length];
        for (int i = 0; i < values.Length; i++)
            list[i] = new(values[i].Key, values[i].Value);
        return list;
    }

    private static KeyValuePair<RedisKey, RedisValue>[] ToRedisPairs(KeyValuePair<string, object>[] values)
    {
        var list = new KeyValuePair<RedisKey, RedisValue>[values.Length];
        for (int i = 0; i < values.Length; i++)
            list[i] = new(values[i].Key, ToRedisValue(values[i].Value));
        return list;
    }

    private static RedisKey[] ToRedisKeys(string[] keys)
    {
        var list = new RedisKey[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            list[i] = keys[i];
        return list;
    }


    public IServer GetServer(string host = null, int port = 0)
    {
        if (host.IsNullOrWhiteSpace() && port <= 0) return GetDefaultServer();

        return _redisServer.GetServer(
            host: host.IsNullOrWhiteSpace() ? _redisSettings?.Server1 : host,
            port: port > 0 ? port : (_redisSettings?.Port1 ?? 6379));
    }

    private IServer GetDefaultServer()
    {
        if (_redisSettings is not null && !_redisSettings.Server1.IsNullOrWhiteSpace())
            return _redisServer.GetServer(_redisSettings.Server1, _redisSettings.Port1);

        return _redisServer.GetServer(_redisServer.GetEndPoints()[0]);
    }

    public IEnumerable<string> GetAllKey(RedisValue[] command, CommandFlags flags = CommandFlags.None)
    {
        var server = GetDefaultServer();
        var keys = server.CommandGetKeys(
            command: command,
            flags: flags);

        foreach (var key in keys)
            yield return key.ToString();
    }

    public IEnumerable<string> GetAllKey(PagingParameter pagingParameter = null, string pattern = null, CommandFlags flags = CommandFlags.None)
    {
        var server = GetDefaultServer();
        var keys = server.Keys(
            database: _redisSettings?.DefaultDatabaseIndex > 0 ? _redisSettings.DefaultDatabaseIndex : _redisCache.Database,
            pattern: pattern.IsNullOrWhiteSpace() ? default : pattern,
            pageSize: pagingParameter is null ? 250 : (pagingParameter.PageSize > 250 ? 250 : pagingParameter.PageSize),
            pageOffset: pagingParameter is null ? 0 : ((pagingParameter.PageNumber - 1) * pagingParameter.PageSize),
            flags: flags);

        foreach (var key in keys)
            yield return key.ToString();
    }


    public string Execute(string command, ICollection<object> args, CommandFlags flags = CommandFlags.None)
    {
        var result = _redisCache.Execute(command, args, flags);
        return result == null ? string.Empty : result.ToString();
    }

    public async Task<string> ExecuteAsync(string command, ICollection<object> args, CommandFlags flags = CommandFlags.None)
    {
        var result = await _redisCache.ExecuteAsync(command, args, flags);
        return result == null ? string.Empty : result.ToString();
    }


    public bool Set(string key, string value, TimeSpan? expiry = null, bool keepTTL = false)
        => _redisCache.StringSet(key, value, expiry, keepTTL);

    public bool Set(string key, object value, TimeSpan? expiry = null, bool keepTTl = false)
       => _redisCache.StringSet(key, ToRedisValue(value), expiry, keepTTl);

    // Fixed: Array.CopyTo between KeyValuePair<string,string>[] and KeyValuePair<RedisKey,RedisValue>[]
    // always threw ArrayTypeMismatchException (no element conversion happens).
    public bool Set(KeyValuePair<string, string>[] values)
        => _redisCache.StringSet(ToRedisPairs(values));

    public bool Set(KeyValuePair<string, object>[] values, CommandFlags flags = CommandFlags.None)
        => _redisCache.StringSet(ToRedisPairs(values), flags: flags);

    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null, bool keepTTL = false)
        => await _redisCache.StringSetAsync(key, value, expiry, keepTTL);

    public async Task<bool> SetAsync(string key, object value, TimeSpan? expiry = null, bool keepTtl = false)
        => await _redisCache.StringSetAsync(key, ToRedisValue(value), expiry, keepTtl);

    public async Task<bool> SetAsync(KeyValuePair<string, string>[] values)
        => await _redisCache.StringSetAsync(ToRedisPairs(values));

    public async Task<bool> SetAsync(KeyValuePair<string, object>[] values)
        => await _redisCache.StringSetAsync(ToRedisPairs(values));


    public string SetAndGet(string key, string value, TimeSpan? expiry = null, bool keepTTl = false)
       => _redisCache.StringSetAndGet(key, value, expiry, keepTTl);

    public string SetAndGet(string key, object value, TimeSpan? expiry = null, bool keepTTl = false)
       => _redisCache.StringSetAndGet(key, ToRedisValue(value), expiry, keepTTl);

    public async Task<string> SetAndGetAsync(string key, string value, TimeSpan? expiry = null, bool keepTTl = false)
       => await _redisCache.StringSetAndGetAsync(key, value, expiry, keepTTl);

    public async Task<string> SetAndGetAsync(string key, object value, TimeSpan? expiry = null, bool keepTTl = false)
       => await _redisCache.StringSetAndGetAsync(key, ToRedisValue(value), expiry, keepTTl);

    public TValue SetAndGet<TValue>(string key, string value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class
    {
        var result = _redisCache.StringSetAndGet(key, value, expiry, keepTTl);
        return FromRedisValue<TValue>(result);
    }

    public TValue SetAndGet<TValue>(string key, object value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class
    {
        var result = _redisCache.StringSetAndGet(key, ToRedisValue(value), expiry, keepTTl);
        return FromRedisValue<TValue>(result);
    }

    public async Task<TValue> SetAndGetAsync<TValue>(string key, string value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class
    {
        var result = await _redisCache.StringSetAndGetAsync(key, value, expiry, keepTTl);
        return FromRedisValue<TValue>(result);
    }

    public async Task<TValue> SetAndGetAsync<TValue>(string key, object value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class
    {
        var result = await _redisCache.StringSetAndGetAsync(key, ToRedisValue(value), expiry, keepTTl);
        return FromRedisValue<TValue>(result);
    }


    public T Get<T>(string key) where T : class
    {
        var value = _redisCache.StringGet(key);
        if (value.IsNullOrEmpty)
            return default;

        return FromRedisValue<T>(value);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var value = await _redisCache.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        return FromRedisValue<T>(value);
    }

    public string Get(string key)
        => _redisCache.StringGet(key);

    // Fixed: string[].CopyTo(RedisKey[]) always threw (no element conversion happens).
    public string[] Get(string[] keys)
        => _redisCache.StringGet(ToRedisKeys(keys)).ToStringArray();

    public async Task<string> GetAsync(string key)
        => await _redisCache.StringGetAsync(key);

    public async Task<string[]> GetAsync(string[] keys)
    {
        var result = await _redisCache.StringGetAsync(ToRedisKeys(keys));
        return result.ToStringArray();
    }


    public string GetAndSet(string key, string value)
        => _redisCache.StringGetSet(key, value);

    public string GetAndSet(string key, object value)
        => _redisCache.StringGetSet(key, ToRedisValue(value));

    public async Task<string> GetAndSetAsync(string key, string value)
        => await _redisCache.StringGetSetAsync(key, value);

    public async Task<string> GetAndSetAsync(string key, object value)
        => await _redisCache.StringGetSetAsync(key, ToRedisValue(value));

    public TValue GetAndSet<TValue>(string key, string value) where TValue : class
    {
        var result = _redisCache.StringGetSet(key, value);
        return FromRedisValue<TValue>(result);
    }

    public TValue GetAndSet<TValue>(string key, object value) where TValue : class
    {
        var result = _redisCache.StringGetSet(key, ToRedisValue(value));
        return FromRedisValue<TValue>(result);
    }

    public async Task<TValue> GetAndSetAsync<TValue>(string key, string value) where TValue : class
    {
        var result = await _redisCache.StringGetSetAsync(key, value);
        return FromRedisValue<TValue>(result);
    }

    public async Task<TValue> GetAndSetAsync<TValue>(string key, object value) where TValue : class
    {
        var result = await _redisCache.StringGetSetAsync(key, ToRedisValue(value));
        return FromRedisValue<TValue>(result);
    }


    public bool Delete(string key)
        => _redisCache.KeyDelete(key);

    public async Task<bool> DeleteAsync(string key)
        => await _redisCache.KeyDeleteAsync(key);

    public void Rename(string oldKey, string newKey)
        => _redisCache.KeyRename(oldKey, newKey);

    public async Task RenameAsync(string oldKey, string newKey)
        => await _redisCache.KeyRenameAsync(oldKey, newKey);

    public bool Exists(string Key)
        => _redisCache.KeyExists(Key);

    public async Task<bool> ExistsAsync(string Key)
        => await _redisCache.KeyExistsAsync(Key);

    public long Length(string Key)
        => _redisCache.StringLength(Key);

    public async Task<long> LengthAsync(string Key)
        => await _redisCache.StringLengthAsync(Key);

    public bool Expire(string Key, TimeSpan? expiry = null)
        => _redisCache.KeyExpire(Key, expiry);

    public async Task<bool> ExpireAsync(string Key, TimeSpan? expiry = null)
        => await _redisCache.KeyExpireAsync(Key, expiry);


    public bool Persist(string Key) =>
        _redisCache.KeyPersist(Key);

    public async Task<bool> PersistAsync(string Key)
        => await _redisCache.KeyPersistAsync(Key);

    public TimeSpan? Idle(string Key)
        => _redisCache.KeyIdleTime(Key);

    public async Task<TimeSpan?> IdleAsync(string Key)
        => await _redisCache.KeyIdleTimeAsync(Key);

    public long Increment(string Key)
        => _redisCache.StringIncrement(Key);

    public async Task<long> IncrementAsync(string Key)
        => await _redisCache.StringIncrementAsync(Key);

    public long Decrement(string Key)
        => _redisCache.StringDecrement(Key);

    public async Task<long> DecrementAsync(string Key)
        => await _redisCache.StringDecrementAsync(Key);
}