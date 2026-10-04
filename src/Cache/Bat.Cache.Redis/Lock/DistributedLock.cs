namespace Bat.Cache.Redis;

public class DistributedLock : IDistributedLock
{
    // Lua scripts that only touch the lock when we still own it (value == our token).
    private const string ReleaseScript = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

    private const string RenewScript = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('expire', KEYS[1], ARGV[2])
            else
                return 0
            end";

    private readonly IRedisCacheProvider _redisCache;

    // ScriptEvaluateAsync uses EVALSHA (script cached on the server) instead of sending the script text on every call.
    // Falls back to a raw EVAL for IRedisCacheProvider implementations/mocks that don't expose _redisCache.
    private async Task<bool> EvaluateAsync(string script, string key, RedisValue[] args)
    {
        var database = _redisCache._redisCache;
        if (database is not null)
            return (long)await database.ScriptEvaluateAsync(script, [key], args) == 1;

        var commandArgs = new List<object> { script, 1, key };
        foreach (var arg in args) commandArgs.Add(arg.ToString());
        return await _redisCache.ExecuteAsync("EVAL", commandArgs) == "1";
    }
    private readonly DistributedLockOptions _defaultOptions;

    public DistributedLock(IRedisCacheProvider redisCache, IOptions<DistributedLockOptions> options = null)
    {
        _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        _defaultOptions = options?.Value ?? new DistributedLockOptions();
    }


    private async Task<bool> TrySetLock(string key, string value, int expirySeconds)
    {
        try
        {
            // SET key value NX EX seconds. Typed API when the database is available; the raw command is kept as a
            // fallback for IRedisCacheProvider implementations/mocks that don't expose _redisCache.
            var database = _redisCache._redisCache;
            if (database is not null)
                return await database.StringSetAsync(key, value, TimeSpan.FromSeconds(expirySeconds), When.NotExists);

            var result = await _redisCache.ExecuteAsync("SET", [key, value, "NX", "EX", expirySeconds]);
            return result == "OK";
        }
        catch
        {
            return false;
        }
    }


    public async Task<bool> IsLocked(string key)
    {
        if (key.IsNullOrWhiteSpace())
            return false;

        var lockKey = $"{_defaultOptions.LockKeyPrefix}:{key}";
        var value = await _redisCache.GetAsync(lockKey);
        return !string.IsNullOrWhiteSpace(value);
    }

    public async Task<DistributedLockResult> SetLock(string key, CancellationToken cancellationToken = default)
    {
        return await SetLock(key, _defaultOptions, cancellationToken);
    }

    public async Task<DistributedLockResult> SetLock(string key, DistributedLockOptions options, CancellationToken cancellationToken = default)
    {
        if (key.IsNullOrWhiteSpace())
            throw new ArgumentNullException(nameof(key));

        var lockKey = $"{options.LockKeyPrefix}:{key}";
        var lockValue = Guid.NewGuid().ToString();
        var acquireTimeout = TimeSpan.FromSeconds(options.AcquireTimeout);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < acquireTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var acquired = await TrySetLock(lockKey, lockValue, options.LockExpiry);

            if (acquired)
            {
                return new DistributedLockResult(this, lockKey, lockValue, options.AutoRenew, options.RenewalInterval, options.LockExpiry);
            }

            await Task.Delay(options.RetryDelay, cancellationToken);
        }

        return null; // Lock could not be acquired within timeout
    }

    public async Task<DistributedLockResult> TrySetLock(string key, int expirySeconds = 30, CancellationToken cancellationToken = default)
    {
        if (key.IsNullOrWhiteSpace())
            throw new ArgumentNullException(nameof(key));

        var lockKey = $"{_defaultOptions.LockKeyPrefix}:{key}";
        var lockValue = Guid.NewGuid().ToString();

        var acquired = await TrySetLock(lockKey, lockValue, expirySeconds);

        return acquired ? new DistributedLockResult(this, lockKey, lockValue, false, 0) : null;
    }


    public async Task<bool> ReleaseLock(string key, string value)
    {
        if (key.IsNullOrWhiteSpace() || value.IsNullOrWhiteSpace())
            return false;

        // Lua script to ensure we only delete the lock if we own it
        try
        {
            return await EvaluateAsync(ReleaseScript, key, [value]);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RenewLock(string key, string value, int expirySeconds = 30)
    {
        if (key.IsNullOrWhiteSpace() || value.IsNullOrWhiteSpace())
            return false;

        // Lua script to renew lock only if we own it
        try
        {
            return await EvaluateAsync(RenewScript, key, [value, expirySeconds]);
        }
        catch
        {
            return false;
        }
    }


    public async Task<bool> ExecuteWithLock(string key, Func<Task> action, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLock(key, action, _defaultOptions, cancellationToken);
    }

    public async Task<bool> ExecuteWithLock(string key, Func<Task> action, DistributedLockOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var lockResult = await SetLock(key, options, cancellationToken);

        if (lockResult == null || !lockResult.IsAcquired)
            return false;

        try
        {
            await action();
            return true;
        }
        finally
        {
            await lockResult.ReleaseAsync();
        }
    }

    public async Task<(bool Success, T Result)> ExecuteWithLock<T>(string key, Func<Task<T>> func, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLock(key, func, _defaultOptions, cancellationToken);
    }

    public async Task<(bool Success, T Result)> ExecuteWithLock<T>(string key, Func<Task<T>> func, DistributedLockOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);

        await using var lockResult = await SetLock(key, options, cancellationToken);
        if (lockResult == null || !lockResult.IsAcquired)
            return (false, default);

        try
        {
            var result = await func();
            return (true, result);
        }
        finally
        {
            await lockResult.ReleaseAsync();
        }
    }
}