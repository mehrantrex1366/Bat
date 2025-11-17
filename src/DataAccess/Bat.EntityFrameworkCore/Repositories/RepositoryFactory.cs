namespace Bat.EntityFrameworkCore;

public class RepositoryFactory(IServiceProvider serviceProvider) : IRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public virtual EFGenericRepo<T> GetRepository<T>() where T : class, IBaseEntity
        => (EFGenericRepo<T>)_serviceProvider.GetService<IEFGenericRepo<T>>();
        // => _serviceProvider.GetRequiredService<EFGenericRepo<T>>();

}