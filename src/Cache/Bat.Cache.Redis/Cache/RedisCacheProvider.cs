namespace Bat.Cache.Redis;

public class RedisCacheProvider : IRedisCacheProvider
{
    public IDatabase _redisCache { get; set; }
    private readonly RedisSettings _redisSettings;
    public ConnectionMultiplexer _redisServer { get; set; }

    public RedisCacheProvider(IOptions<RedisSettings> redisSettings)
    {
        _redisSettings = redisSettings.Value;
        if (_redisSettings.Server1.IsNullOrWhiteSpace() ||
            _redisSettings.Server2.IsNullOrWhiteSpace() ||
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

        config.Ssl = _redisSettings.SslSettings?.UseSsl ?? false;
        config.SslHost = _redisSettings.SslSettings.Host ?? null;
        config.SslProtocols = _redisSettings.SslSettings.Protocol;

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


    public IServer GetServer(string host = null, int port = 0)
        => _redisServer.GetServer(
            host: host.IsNullOrWhiteSpace() ? _redisSettings.Server1 : host,
            port: port > 0 ? _redisSettings.Port1 : port);

    public IEnumerable<string> GetAllKey(RedisValue[] command, CommandFlags flags = CommandFlags.None)
    {
        var server = _redisServer.GetServer(_redisSettings.Server1, _redisSettings.Port1);
        var keys = server.CommandGetKeys(
            command: command,
            flags: flags);

        foreach (var key in keys)
            yield return key.ToString();
    }

    public IEnumerable<string> GetAllKey(PagingParameter pagingParameter = null, string pattern = null, CommandFlags flags = CommandFlags.None)
    {
        var server = _redisServer.GetServer(_redisSettings.Server1, _redisSettings.Port1);
        var keys = server.Keys(
            database: _redisSettings.DefaultDatabaseIndex > 0 ? _redisSettings.DefaultDatabaseIndex : 0,
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
       => _redisCache.StringSet(key, value.SerializeToJson(), expiry, keepTTl);

    public bool Set(KeyValuePair<string, string>[] values)
    {
        var list = new KeyValuePair<RedisKey, RedisValue>[values.Length];
        values.CopyTo(list, 0);
        return _redisCache.StringSet(list);
    }

    public bool Set(KeyValuePair<string, object>[] values, CommandFlags flags = CommandFlags.None)
    {
        var list = values.Select(x => new
            KeyValuePair<RedisKey, RedisValue>(x.Key, x.Value.SerializeToJson()))
            .ToArray();
        return _redisCache.StringSet(list);
    }

    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null, bool keepTTL = false)
        => await _redisCache.StringSetAsync(key, value, expiry, keepTTL);

    public async Task<bool> SetAsync(string key, object value, TimeSpan? expiry = null, bool keepTtl = false)
        => await _redisCache.StringSetAsync(key, value.SerializeToJson(), expiry, keepTtl);

    public async Task<bool> SetAsync(KeyValuePair<string, string>[] values)
    {
        var list = new KeyValuePair<RedisKey, RedisValue>[values.Length];
        values.CopyTo(list, 0);
        return await _redisCache.StringSetAsync(list);
    }

    public async Task<bool> SetAsync(KeyValuePair<string, object>[] values)
    {
        var list = values.Select(x => new
            KeyValuePair<RedisKey, RedisValue>(x.Key, x.Value.SerializeToJson()))
            .ToArray();
        return await _redisCache.StringSetAsync(list);
    }


    public string SetAndGet(string key, string value, TimeSpan? expiry = null, bool keepTTl = false)
       => _redisCache.StringSetAndGet(key, value, expiry, keepTTl);

    public string SetAndGet(string key, object value, TimeSpan? expiry = null, bool keepTTl = false)
       => _redisCache.StringSetAndGet(key, value.SerializeToJson(), expiry, keepTTl);

    public async Task<string> SetAndGetAsync(string key, string value, TimeSpan? expiry = null, bool keepTTl = false)
       => await _redisCache.StringSetAndGetAsync(key, value, expiry, keepTTl);

    public async Task<string> SetAndGetAsync(string key, object value, TimeSpan? expiry = null, bool keepTTl = false)
       => await _redisCache.StringSetAndGetAsync(key, value.SerializeToJson(), expiry, keepTTl);

    public TValue SetAndGet<TValue>(string key, string value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class
    {
        var result = _redisCache.StringSetAndGet(key, value, expiry, keepTTl);
        return result.ToString().DeSerializeJson<TValue>();
    }

    public TValue SetAndGet<TValue>(string key, object value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class
    {
        var result = _redisCache.StringSetAndGet(key, value.SerializeToJson(), expiry, keepTTl);
        return result.ToString().DeSerializeJson<TValue>();
    }

    public async Task<TValue> SetAndGetAsync<TValue>(string key, string value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class
    {
        var result = await _redisCache.StringSetAndGetAsync(key, value, expiry, keepTTl);
        return result.ToString().DeSerializeJson<TValue>();
    }

    public async Task<TValue> SetAndGetAsync<TValue>(string key, object value, TimeSpan? expiry = null, bool keepTTl = false) where TValue : class
    {
        var result = await _redisCache.StringSetAndGetAsync(key, value.SerializeToJson(), expiry, keepTTl);
        return result.ToString().DeSerializeJson<TValue>();
    }


    public T Get<T>(string key) where T : class
    {
        var value = _redisCache.StringGet(key);
        if (value.IsNullOrEmpty)
            return default;

        return value.ToString().DeSerializeJson<T>();
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var value = await _redisCache.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        return value.ToString().DeSerializeJson<T>();
    }

    public string Get(string key)
        => _redisCache.StringGet(key);

    public string[] Get(string[] keys)
    {
        var list = new RedisKey[keys.Length];
        keys.CopyTo(list, 0);
        return _redisCache.StringGet(list).ToStringArray();
    }

    public async Task<string> GetAsync(string key)
        => await _redisCache.StringGetAsync(key);

    public async Task<string[]> GetAsync(string[] keys)
    {
        var list = new RedisKey[keys.Length];
        keys.CopyTo(list, 0);
        var result = await _redisCache.StringGetAsync(list);
        return result.ToStringArray();
    }


    public string GetAndSet(string key, string value)
        => _redisCache.StringGetSet(key, value);

    public string GetAndSet(string key, object value)
        => _redisCache.StringGetSet(key, value.SerializeToJson());

    public async Task<string> GetAndSetAsync(string key, string value)
        => await _redisCache.StringGetSetAsync(key, value);

    public async Task<string> GetAndSetAsync(string key, object value)
        => await _redisCache.StringGetSetAsync(key, value.SerializeToJson());

    public TValue GetAndSet<TValue>(string key, string value) where TValue : class
    {
        var result = _redisCache.StringGetSet(key, value);
        return result.ToString().DeSerializeJson<TValue>();
    }

    public TValue GetAndSet<TValue>(string key, object value) where TValue : class
    {
        var result = _redisCache.StringGetSet(key, value.SerializeToJson());
        return result.ToString().DeSerializeJson<TValue>();
    }

    public async Task<TValue> GetAndSetAsync<TValue>(string key, string value) where TValue : class
    {
        var result = await _redisCache.StringGetSetAsync(key, value);
        return result.ToString().DeSerializeJson<TValue>();
    }

    public async Task<TValue> GetAndSetAsync<TValue>(string key, object value) where TValue : class
    {
        var result = await _redisCache.StringGetSetAsync(key, value.SerializeToJson());
        return result.ToString().DeSerializeJson<TValue>();
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