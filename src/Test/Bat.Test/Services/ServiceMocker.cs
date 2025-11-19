namespace Bat.Test;

public class ServiceMocker<TUow, TDbContext, TService>
    where TUow : class, IBatUnitOfWork
    where TDbContext : DbContext, IBatDbContext
    where TService : IScopedInjection
{
    protected ServiceBuilder<TService> ServiceBuilder;
    protected StaticValuesBuilder StaticValuesBuilder = new();
    protected MockUowBuilder<TUow, TDbContext> UnitOfWork;
    
    protected TService Service => ServiceBuilder.Build();

    protected ServiceMocker()
    {
        UnitOfWork = new MockUowBuilder<TUow, TDbContext>();
        UnitOfWork.Mock();

        ServiceBuilder = new ServiceBuilder<TService>()
            .Inject(UnitOfWork.AsMock().Object);
    }
}