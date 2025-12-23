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

    public DistributedLockResult(IDistributedLock distributedLock, string lockKey, string lockValue,
        bool autoRenew, int renewalInterval)
    {
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
                    await _distributedLock.RenewLock(_lockKey, _lockValue);
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


    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ReleaseAsync().GetAwaiter().GetResult();
        _renewalCancellation?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await ReleaseAsync();
        _renewalCancellation?.Dispose();
        GC.SuppressFinalize(this);
    }
}