namespace Bat.Test;

public interface IInitialDbBuilder<T> where T : BatDbContext
{
    IBehaviorDbBuilder<T> Mock();
    
    IBehaviorDbBuilder<T> MockWithDbSet<TEntity>(Expression<Func<T, DbSet<TEntity>>> expression, List<TEntity> entities) where TEntity : class, IBaseEntity;
}