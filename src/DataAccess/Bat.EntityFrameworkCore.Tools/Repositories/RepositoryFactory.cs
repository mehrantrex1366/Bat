namespace Bat.EntityFrameworkCore.Tools;

public class RepositoryFactory(IServiceProvider serviceProvider) : IRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;


    public virtual EFGenericRepo<T> GetRepository<T>() where T : class, IBaseEntity
        => (EFGenericRepo<T>)_serviceProvider.GetService<IEFGenericRepo<T>>();

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