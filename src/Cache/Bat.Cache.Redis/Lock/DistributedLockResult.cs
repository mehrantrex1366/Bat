namespace Bat.Cache.Redis;

public class DistributedLockResult : IDisposable, IAsyncDisposable
{
    private bool _disposed;
    private readonly string _lockKey;
    private readonly string _lockValue;
    private readonly Task _renewalTask;
    private readonly IDistributedLock _distributedLock;
    private readonly CancellationTokenSource _renewalCancellation;

    public bool IsAcquired { get; private set; }

    private readonly int _lockExpirySeconds = 30;

    public DistributedLockResult(IDistributedLock distributedLock, string lockKey, string lockValue,
        bool autoRenew, int renewalInterval)
        : this(distributedLock, lockKey, lockValue, autoRenew, renewalInterval, 30)
    { }

    /// <param name="lockExpirySeconds">Expiry used when the lock is auto-renewed (previously always 30s, ignoring LockExpiry).</param>
    public DistributedLockResult(IDistributedLock distributedLock, string lockKey, string lockValue,
        bool autoRenew, int renewalInterval, int lockExpirySeconds)
    {
        _lockExpirySeconds = lockExpirySeconds > 0 ? lockExpirySeconds : 30;
        IsAcquired = true;
        _lockKey = lockKey;
        _lockValue = lockValue;
        _distributedLock = distributedLock;

        if (autoRenew)
        {
            _renewalCancellation = new CancellationTokenSource();
            _renewalTask = RenewLockPeriodically(renewalInterval, _renewalCancellation.Token);
        }
    }


    private async Task RenewLockPeriodically(int intervalSeconds, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    await _distributedLock.RenewLock(_lockKey, _lockValue, _lockExpirySeconds);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when lock is being disposed
        }
        catch (Exception)
        {
            // Log error but don't throw - renewal failure shouldn't crash the application
        }
    }


    public async Task ReleaseAsync()
    {
        if (_disposed || !IsAcquired) return;

        _renewalCancellation?.Cancel();
        if (_renewalTask != null)
        {
            try
            {
                await _renewalTask;
            }
            catch
            {
                // Ignore renewal task exceptions during disposal
            }
        }

        await _distributedLock.ReleaseLock(_lockKey, _lockValue);
        IsAcquired = false;
    }


    // Fixed: _disposed was set BEFORE calling ReleaseAsync, and ReleaseAsync returns early when _disposed is true,
    // so disposing (`using` / `await using`) never released the lock — it stayed held until it expired.
    public void Dispose()
    {
        if (_disposed) return;

        ReleaseAsync().GetAwaiter().GetResult();
        _disposed = true;
        _renewalCancellation?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await ReleaseAsync();
        _disposed = true;
        _renewalCancellation?.Dispose();
        GC.SuppressFinalize(this);
    }
}