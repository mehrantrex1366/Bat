# For use Bat.Cache.Redis just do it :

## Distributed Cache

1- Install Bat.Cache.Redis on your project

2- Set RedisSettings on appSettings.json file
for example :


    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft": "Warning",
          "Microsoft.Hosting.Lifetime": "Information"
        }
      },
      "AllowedHosts": "*",
      "RedisSettings": {

        "Server1": "127.0.0.1",
        "Port1": 6379,
        "Server2": "127.0.0.1",
        "Port2": 6380,
        "Server3": "127.0.0.1",
        "Port3": 6381,

        "Username": "",
        "Password": "Norouzi",
        "ClientName": "Bat.Cache.Redis",
        "AbortOnConnectFail": false,
        "SyncTimeout": 5,
        "ConnectRetry": 3,
        "ConnectTimeout": 3000,
        "DefaultDatabaseIndex": 0,
        "IsSentinelConnect": false,
        "AllowAdminCommand": false,
        "IncludeDetailInExceptions": false,
        "CheckCertificateRevocation": false,

        "SslSettings": {

          "UseSsl": false,
          "SslHost": ""
        }

      }
    }


3- Register RedisCacheProvider to service container
for example :


    builder.Services.AddDistributedMemoryCache();
    builder.Services.Configure<RedisSettings>(configuration.GetSection("RedisSettings"));
    builder.Services.AddSingleton<IRedisCacheProvider, RedisCacheProvider>();


4- Use it in business logic
for example :


    public class BaseService : IBaseService
    {
        private readonly IRedisCacheProvider _cacheProvider;

        public BaseService(IRedisCacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }


        public List<string> GetRegions()
        {
            var data = new List<string> { "Tehran", "Esfahan", "Shiraz" };
            _cacheProvider.Set("Regions_Key", data.SerializeToJson(), TimeSpan.FromHours(24));

            return _cacheProvider.Get<List<Region>>("Regions_Key");
        }
    }


 ## Distributed Lock
    services.AddBatDistributedLock(options =>
    {
        options.AcquireTimeout = 10;      // Maximum wait time for lock
        options.LockExpiry = 30;          // Lock expiration time
        options.RetryDelay = 100;         // Retry delay in milliseconds
        options.AutoRenew = false;        // Enable auto-renewal
        options.RenewalInterval = 10;     // Auto-renewal interval
        options.LockKeyPrefix = "myapp";  // Custom prefix for lock keys
    });

    ```csharp
    public async Task<decimal> CalculateBalanceAsync(int accountId)
    {
        var (success, balance) = await _distributedLock.ExecuteWithLock(
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