namespace Bat.EntityFrameworkCore.Tools;

public class BulkRepositoryFactory(IServiceProvider serviceProvider) : IBulkRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;


    public virtual EFBulkGenericRepo<T> GetBulkRepository<T>() where T : class, IBaseEntity
        => (EFBulkGenericRepo<T>)_serviceProvider.GetService<IEFBulkGenericRepo<T>>();

    // Fixed: Dispose() called this.Dispose() and DisposeAsync() returned DisposeAsync() — infinite recursion,
    // i.e. a StackOverflowException that kills the process as soon as the DI scope disposes the factory.
    // The factory owns nothing (repositories belong to the DI container), so there is nothing to release.
    public void Dispose() => GC.SuppressFinalize(this);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}