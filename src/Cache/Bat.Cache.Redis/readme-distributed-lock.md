# Bat Distributed Lock

یک پیاده‌سازی کامل و حرفه‌ای از Distributed Locking با استفاده از Redis.

## ویژگی‌ها

✅ **Thread-Safe و Multi-Instance Safe**  
✅ **Auto-Renewal** برای عملیات‌های طولانی‌مدت  
✅ **Timeout و Retry Mechanism**  
✅ **Async/Await Pattern**  
✅ **IDisposable و IAsyncDisposable Support**  
✅ **Extension Methods** برای استفاده راحت‌تر  
✅ **Lua Scripts** برای atomicity  

---

## نصب و راه‌اندازی

### 1. افزودن سرویس به DI Container

```csharp
// در Startup.cs یا Program.cs
services.AddBatDistributedLock(options =>
{
    options.AcquireTimeout = 10;      // Maximum wait time for lock
    options.LockExpiry = 30;          // Lock expiration time
    options.RetryDelay = 100;         // Retry delay in milliseconds
    options.AutoRenew = false;        // Enable auto-renewal
    options.RenewalInterval = 10;     // Auto-renewal interval
    options.LockKeyPrefix = "myapp";  // Custom prefix for lock keys
});
```

---

## نحوه استفاده

### 1. استفاده پایه با Using Statement

```csharp
public class OrderService
{
    private readonly IDistributedLock _distributedLock;

    public OrderService(IDistributedLock distributedLock)
    {
        _distributedLock = distributedLock;
    }

    public async Task ProcessOrderAsync(int orderId)
    {
        var lockKey = $"order:{orderId}";
        
        await using var lockResult = await _distributedLock.AcquireLockAsync(lockKey);
        
        if (lockResult == null || !lockResult.IsAcquired)
        {
            // Lock could not be acquired
            throw new Exception("Order is being processed by another instance");
        }

        // Your business logic here
        await UpdateOrderStatus(orderId);
        await SendNotification(orderId);
        
        // Lock will be automatically released when disposed
    }
}
```

### 2. استفاده با ExecuteWithLock (ساده‌تر)

```csharp
public async Task ProcessPaymentAsync(string paymentId)
{
    var success = await _distributedLock.ExecuteWithLockAsync(
        key: $"payment:{paymentId}",
        action: async () =>
        {
            // Your logic runs here with lock protection
            await ChargeCustomer(paymentId);
            await UpdateDatabase(paymentId);
        }
    );

    if (!success)
    {
        throw new Exception("Could not acquire lock for payment processing");
    }
}
```

### 3. استفاده با Return Value

```csharp
public async Task<decimal> CalculateBalanceAsync(int accountId)
{
    var (success, balance) = await _distributedLock.ExecuteWithLockAsync(
        key: $"account:{accountId}",
        func: async () =>
        {
            var transactions = await GetTransactions(accountId);
            return transactions.Sum(t => t.Amount);
        }
    );

    if (!success)
    {
        throw new Exception("Could not acquire lock for balance calculation");
    }

    return balance;
}
```

### 4. استفاده با Extension Methods

```csharp
// WithLockAsync
var success = await _distributedLock.WithLockAsync("resource:123", async () =>
{
    await DoSomething();
});

// WithLockOrDefaultAsync
var result = await _distributedLock.WithLockOrDefaultAsync(
    "resource:123",
    async () => await GetValue(),
    defaultValue: 0
);

// WithLockOrThrowAsync
await _distributedLock.WithLockOrThrowAsync(
    "resource:123",
    async () => await ProcessCriticalOperation(),
    errorMessage: "Critical operation failed: lock not acquired"
);
```

### 5. استفاده با Custom Options

```csharp
public async Task LongRunningOperationAsync()
{
    var customOptions = new DistributedLockOptions
    {
        AcquireTimeout = 5,        // Wait 5 seconds max
        LockExpiry = 120,          // Lock expires after 2 minutes
        AutoRenew = true,          // Enable auto-renewal
        RenewalInterval = 30       // Renew every 30 seconds
    };

    await using var lockResult = await _distributedLock.AcquireLockAsync(
        "long-operation", 
        customOptions
    );

    if (lockResult?.IsAcquired == true)
    {
        // Lock will be automatically renewed every 30 seconds
        await ProcessLongRunningTask();
    }
}
```

### 6. Try-Lock Pattern (بدون انتظار)

```csharp
public async Task<bool> TryProcessAsync(string resourceId)
{
    await using var lockResult = await _distributedLock.TryAcquireLockAsync(
        $"resource:{resourceId}",
        expirySeconds: 60
    );

    if (lockResult == null)
    {
        // Lock is already held by another process
        return false;
    }

    // Process immediately without waiting
    await DoWork(resourceId);
    return true;
}
```

### 7. بررسی وضعیت Lock

```csharp
public async Task<bool> IsResourceBusyAsync(string resourceId)
{
    var isLocked = await _distributedLock.IsLockedAsync($"resource:{resourceId}");
    return isLocked;
}
```

---

## سناریوهای کاربردی

### 1. جلوگیری از Duplicate Processing

```csharp
public async Task ProcessJobAsync(string jobId)
{
    await _distributedLock.WithLockOrThrowAsync(
        $"job:{jobId}",
        async () =>
        {
            var job = await _jobRepository.GetAsync(jobId);
            if (job.IsProcessed) return;

            await ProcessJobInternal(job);
            job.IsProcessed = true;
            await _jobRepository.UpdateAsync(job);
        },
        errorMessage: $"Job {jobId} is already being processed"
    );
}
```

### 2. Rate Limiting

```csharp
public async Task<bool> TryExecuteRateLimitedAsync(string userId, Func<Task> action)
{
    var lockKey = $"ratelimit:{userId}:{DateTime.UtcNow:yyyyMMddHHmm}";
    
    await using var lockResult = await _distributedLock.TryAcquireLockAsync(lockKey, 60);
    
    if (lockResult == null)
    {
        return false; // Rate limit exceeded
    }

    await action();
    return true;
}
```

### 3. Critical Section در Distributed System

```csharp
public async Task UpdateInventoryAsync(int productId, int quantity)
{
    await _distributedLock.ExecuteWithLockAsync(
        $"inventory:{productId}",
        async () =>
        {
            var product = await _db.Products.FindAsync(productId);
            
            if (product.Stock < quantity)
                throw new InvalidOperationException("Insufficient stock");

            product.Stock -= quantity;
            await _db.SaveChangesAsync();
        },
        new DistributedLockOptions { LockExpiry = 10 }
    );
}
```

### 4. Scheduled Tasks با Single Instance

```csharp
public async Task RunScheduledTaskAsync()
{
    var lockKey = $"scheduled-task:{DateTime.UtcNow:yyyyMMddHH}";
    
    // Only one instance across all servers will execute this
    var executed = await _distributedLock.ExecuteWithLockAsync(
        lockKey,
        async () => await ExecuteTask(),
        new DistributedLockOptions 
        { 
            AcquireTimeout = 1, // Don't wait, fail fast
            LockExpiry = 3600   // Lock for 1 hour
        }
    );

    if (!executed)
    {
        _logger.LogInformation("Task already running on another instance");
    }
}
```

---

## Best Practices

### ✅ انجام دهید:

1. **همیشه از using statement استفاده کنید** تا lock به درستی release شود
2. **Lock key های مناسب انتخاب کنید** که collision نداشته باشند
3. **Timeout مناسب تنظیم کنید** بر اساس نیاز business
4. **Auto-renewal را فقط برای عملیات طولانی فعال کنید**
5. **Exception handling مناسب داشته باشید**

### ❌ انجام ندهید:

1. **Lock را برای مدت طولانی نگه ندارید** (deadlock risk)
2. **از lock برای کش کردن استفاده نکنید** (از Redis cache استفاده کنید)
3. **Nested locks استفاده نکنید** (deadlock risk)
4. **Lock expiry را خیلی کوتاه تنظیم نکنید** (ممکن است عملیات تمام نشده lock expire شود)

---

## Performance Considerations

- از Lua scripts استفاده می‌شود برای atomic operations
- Lock acquisition در O(1) انجام می‌شود
- Auto-renewal در background thread جداگانه اجرا می‌شود
- هیچ polling یا busy-waiting وجود ندارد

---

## Thread Safety

این implementation کاملاً thread-safe است و می‌تواند در محیط‌های distributed با چندین instance استفاده شود.

---

## مستندات بیشتر

برای اطلاعات بیشتر در مورد Redis locks:
- [Redis SETNX documentation](https://redis.io/commands/setnx/)
- [RedLock algorithm](https://redis.io/topics/distlock)
