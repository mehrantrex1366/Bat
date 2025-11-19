namespace Bat.Test;

public class EasyServiceMocker<TService> : ServiceMocker<IBatUnitOfWork, BatDbContext, TService>
    where TService : IScopedInjection
{
}