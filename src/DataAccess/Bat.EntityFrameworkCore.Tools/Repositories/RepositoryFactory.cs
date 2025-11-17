namespace Bat.EntityFrameworkCore.Tools;

public class RepositoryFactory(IServiceProvider serviceProvider) : IRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;


    public virtual EFGenericRepo<T> GetRepository<T>() where T : class, IBaseEntity
        => (EFGenericRepo<T>)_serviceProvider.GetService<IEFGenericRepo<T>>();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return DisposeAsync();
    }
}