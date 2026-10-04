namespace Bat.EntityFrameworkCore.Tools;

/// <summary>
/// Returns ONE cached DbContext instance per (context type, connection string) for the whole process.
/// </summary>
/// <remarks>
/// WARNING: DbContext is not thread-safe and its change tracker grows forever. A process-wide instance must not be
/// used concurrently (e.g. from ASP.NET Core requests). Prefer DI (AddDbContext / AddDbContextPool / IDbContextFactory).
/// Fixes compared to the previous version:
/// <list type="bullet">
/// <item>a new DbContext was constructed on every call and thrown away (undisposed) when a cached one existed;</item>
/// <item>one static SqlConnection was shared by all contexts, so the connection string passed to the second
/// overload was ignored if any context had been created before;</item>
/// <item>the parameterless overload used a connection string field that was never assigned (always null).</item>
/// </list>
/// </remarks>
public static class DbContextFactory
{
    private static readonly object _lock = new();
    public static readonly string _connectionString;
    private static readonly Dictionary<string, object> _contextPool = [];

    public static BatDbContext GetInstance<TDbContext>() where TDbContext : BatDbContext, new()
        => GetInstance<TDbContext>(_connectionString);

    public static BatDbContext GetInstance<TDbContext>(string connectionString) where TDbContext : BatDbContext, new()
    {
        var key = $"{typeof(TDbContext).FullName}|{connectionString}";
        lock (_lock)
        {
            if (_contextPool.TryGetValue(key, out object pooledContext)) return (TDbContext)pooledContext;

            var connectionOptions = new DbContextOptionsBuilder<TDbContext>()
                         .UseSqlServer(connectionString).Options;
            var context = (TDbContext)Activator.CreateInstance(typeof(TDbContext), connectionOptions);

            _contextPool.Add(key, context);
            return context;
        }
    }
}
