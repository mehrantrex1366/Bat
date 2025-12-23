namespace Bat.Cache.Redis;

public class DistributedLockOptions
{
    /// <summary>
    /// Maximum time to wait for acquiring the lock (in seconds)
    /// </summary>
    public int AcquireTimeout { get; set; } = 10;

    /// <summary>
    /// Lock expiration time (in seconds)
    /// </summary>
    public int LockExpiry { get; set; } = 30;

    /// <summary>
    /// Retry delay when lock acquisition fails (in milliseconds)
    /// </summary>
    public int RetryDelay { get; set; } = 100;

    /// <summary>
    /// Enable auto-renewal of lock for long-running operations
    /// </summary>
    public bool AutoRenew { get; set; } = false;

    /// <summary>
    /// Auto-renewal interval (in seconds) - should be less than LockExpiry
    /// </summary>
    public int RenewalInterval { get; set; } = 10;

    /// <summary>
    /// Prefix for lock keys
    /// </summary>
    public string LockKeyPrefix { get; set; } = "distributed-lock";
}