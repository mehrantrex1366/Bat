namespace Bat.Cache.Redis;

public interface IDistributedLock
{
    /// <summary>
    /// Attempts to acquire a distributed lock
    /// </summary>
    /// <param name="key">Unique key for the lock</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LockResult containing lock information, or null if lock couldn't be acquired</returns>
    Task<DistributedLockResult> SetLock(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a distributed lock with custom options
    /// </summary>
    /// <param name="key">Unique key for the lock</param>
    /// <param name="options">Lock configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LockResult containing lock information, or null if lock couldn't be acquired</returns>
    Task<DistributedLockResult> SetLock(string key, DistributedLockOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a lock immediately without retrying
    /// </summary>
    /// <param name="key">Unique key for the lock</param>
    /// <param name="expirySeconds">Lock expiration in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LockResult if acquired, null otherwise</returns>
    Task<DistributedLockResult> TrySetLock(string key, int expirySeconds = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a previously acquired lock
    /// </summary>
    /// <param name="key">Lock key</param>
    /// <param name="value">Lock value (token)</param>
    /// <returns>True if lock was released, false otherwise</returns>
    Task<bool> ReleaseLock(string key, string value);

    /// <summary>
    /// Renews the expiration time of an existing lock
    /// </summary>
    /// <param name="key">Lock key</param>
    /// <param name="value">Lock value (token)</param>
    /// <param name="expirySeconds">New expiration time in seconds</param>
    /// <returns>True if lock was renewed, false otherwise</returns>
    Task<bool> RenewLock(string key, string value, int expirySeconds = 30);

    /// <summary>
    /// Executes an action within a distributed lock
    /// </summary>
    /// <param name="key">Unique key for the lock</param>
    /// <param name="action">Action to execute while holding the lock</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if action was executed, false if lock couldn't be acquired</returns>
    Task<bool> ExecuteWithLock(string key, Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action within a distributed lock with custom options
    /// </summary>
    /// <param name="key">Unique key for the lock</param>
    /// <param name="action">Action to execute while holding the lock</param>
    /// <param name="options">Lock configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if action was executed, false if lock couldn't be acquired</returns>
    Task<bool> ExecuteWithLock(string key, Func<Task> action, DistributedLockOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function within a distributed lock and returns the result
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="key">Unique key for the lock</param>
    /// <param name="func">Function to execute while holding the lock</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing success status and result</returns>
    Task<(bool Success, T Result)> ExecuteWithLock<T>(string key, Func<Task<T>> func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function within a distributed lock and returns the result with custom options
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="key">Unique key for the lock</param>
    /// <param name="func">Function to execute while holding the lock</param>
    /// <param name="options">Lock configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing success status and result</returns>
    Task<(bool Success, T Result)> ExecuteWithLock<T>(string key, Func<Task<T>> func, DistributedLockOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a lock currently exists
    /// </summary>
    /// <param name="key">Lock key</param>
    /// <returns>True if lock exists, false otherwise</returns>
    Task<bool> IsLocked(string key);
}