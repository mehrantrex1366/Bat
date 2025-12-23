namespace Bat.Cache.Redis;

public class DistributedLock : IDistributedLock
{
    private readonly IRedisCacheProvider _redisCache;
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
            // SET key value NX EX seconds
            // NX: Only set if key doesn't exist
            // EX: Set expiry time in seconds
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
                return new DistributedLockResult(this, lockKey, lockValue, options.AutoRenew, options.RenewalInterval);
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
        const string script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

        try
        {
            var result = await _redisCache.ExecuteAsync("EVAL", [script, 1, key, value]);
            return result == "1";
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
        const string script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('expire', KEYS[1], ARGV[2])
            else
                return 0
            end";

        try
        {
            var result = await _redisCache.ExecuteAsync("EVAL", [script, 1, key, value, expirySeconds]);
            return result == "1";
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