namespace Bat.Test;

public interface IBehaviorRepoBuilder<TDbContext, TEntity>
    where TDbContext : DbContext, IBatDbContext
    where TEntity : class, IBaseEntity
{
    IBehaviorRepoBuilder<TDbContext, TEntity> Setup(Expression<Action<EFGenericRepo<TEntity>>> expression);
    IBehaviorRepoBuilder<TDbContext, TEntity> SetupPartialUpdate();
    IBehaviorRepoBuilder<TDbContext, TEntity> VerifyPartialUpdate(Times callTimes);
    IBehaviorRepoBuilder<TDbContext, TEntity> SetAsNoTracking();
    EFGenericRepo<TEntity> Build();
    Mock<EFGenericRepo<TEntity>> AsMock();
}