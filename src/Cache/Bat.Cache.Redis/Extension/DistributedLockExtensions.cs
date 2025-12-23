namespace Bat.Cache.Redis;

public static class DistributedLockExtensions
{
    /// <summary>
    /// Adds distributed locking services to the service collection
    /// </summary>
    public static IServiceCollection AddBatDistributedLock(this IServiceCollection services, Action<DistributedLockOptions> configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<DistributedLockOptions>(options => { });
        }

        services.AddSingleton<IDistributedLock, DistributedLock>();

        return services;
    }


    /// <summary>
    /// Executes an action with a distributed lock using a simpler syntax
    /// </summary>
    public static async Task<bool> WithLockAsync(this IDistributedLock distributedLock, string key, Func<Task> action)
    {
        return await distributedLock.ExecuteWithLock(key, action);
    }

    /// <summary>
    /// Executes a function with a distributed lock using a simpler syntax
    /// </summary>
    public static async Task<(bool Success, T Result)> WithLockAsync<T>(this IDistributedLock distributedLock, string key, Func<Task<T>> func)
    {
        return await distributedLock.ExecuteWithLock(key, func);
    }

    /// <summary>
    /// Tries to execute an action with lock, returns default if lock cannot be acquired
    /// </summary>
    public static async Task<T> WithLockOrDefaultAsync<T>(this IDistributedLock distributedLock, string key, Func<Task<T>> func, T defaultValue = default)
    {
        var (success, result) = await distributedLock.ExecuteWithLock(key, func);
        return success ? result : defaultValue;
    }

    /// <summary>
    /// Executes an action with lock or throws exception if lock cannot be acquired
    /// </summary>
    public static async Task WithLockOrThrow(this IDistributedLock distributedLock, string key, Func<Task> action, string errorMessage = null)
    {
        var success = await distributedLock.ExecuteWithLock(key, action);
        if (success is false)
            throw new InvalidOperationException(errorMessage ?? $"Failed to acquire lock for key: {key}");
    }

    /// <summary>
    /// Executes a function with lock or throws exception if lock cannot be acquired
    /// </summary>
    public static async Task<T> WithLockOrThrow<T>(this IDistributedLock distributedLock, string key, Func<Task<T>> func, string errorMessage = null)
    {
        var (success, result) = await distributedLock.ExecuteWithLock(key, func);
        if (success is false)
            throw new InvalidOperationException(errorMessage ?? $"Failed to acquire lock for key: {key}");

        return result;
    }
}
