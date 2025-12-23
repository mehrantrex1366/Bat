namespace Bat.Test;

public static class TestTools
{
    public static MockBuilder<T> GetMock<T>() where T : class
        => new();
    
    public static MockDbContextBuilder<TDbContext> GetMockDbContext<TDbContext>() where TDbContext : BatDbContext
        => new();

    public static IInitialUowBuilder<TUow, TDbContext> GetMockUnitOfWork<TUow, TDbContext>()
         where TUow : class, IBatUnitOfWork
         where TDbContext : DbContext, IBatDbContext
        => new MockUowBuilder<TUow, TDbContext>();


    public static NSubstituteBuilder<T> GetNSubstitute<T>() where T : class
        => new();

    public static NSubstituteDbBuilder<TDbContext> GetNSubstituteDbContext<TDbContext>() where TDbContext : BatDbContext
        => new();

    public static IInitialUowBuilder<TUow, TDbContext> GetNSubstituteUnitOfWork<TUow, TDbContext>()
         where TUow : class, IBatUnitOfWork
         where TDbContext : DbContext, IBatDbContext
        => throw new NotImplementedException();


    public static ServiceBuilder<TService> GetService<TService>() where TService : IScopedInjection
        => new();

    public static GeneralBogusBuilder<T> ObjectFaker<T>() where T : class
        => new();
}