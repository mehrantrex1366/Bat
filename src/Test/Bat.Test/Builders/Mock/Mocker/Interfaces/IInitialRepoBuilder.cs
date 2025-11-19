namespace Bat.Test;

public interface IInitialRepoBuilder<TDbContext, TEntity>
    where TDbContext : DbContext, IBatDbContext
    where TEntity : class, IBaseEntity
{
    IBehaviorRepoBuilder<TDbContext, TEntity> MockWithDataBase(Mock<TDbContext> dbMock);
}