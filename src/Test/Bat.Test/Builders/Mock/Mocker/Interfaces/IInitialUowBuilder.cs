namespace Bat.Test;

public interface IInitialUowBuilder<TUow, TDbContext>
    where TUow : class, IBatUnitOfWork
    where TDbContext : DbContext, IBatDbContext
{
    IBehaviorUowBuilder<TUow, TDbContext> Mock();

    IBehaviorUowBuilder<TUow, TDbContext> MockWithDbSet<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> expression, List<TEntity> entities)
        where TEntity : class, IBaseEntity;

    IBehaviorUowBuilder<TUow, TDbContext> MockWithRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression, List<TEntity> entities)
        where TEntity : class, IBaseEntity;

    IBehaviorUowBuilder<TUow, TDbContext> MockWithRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression)
    where TEntity : class, IBaseEntity;

    IBehaviorUowBuilder<TUow, TDbContext> MockWithReadOnlyRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression, List<TEntity> entities)
    where TEntity : class, IBaseEntity;

    public IBehaviorUowBuilder<TUow, TDbContext> MockWithReadOnlyRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression)
    where TEntity : class, IBaseEntity;
}