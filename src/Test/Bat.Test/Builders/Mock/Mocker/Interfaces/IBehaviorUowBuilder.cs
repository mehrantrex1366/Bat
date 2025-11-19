namespace Bat.Test;

public interface IBehaviorUowBuilder<TUow, TDbContext>
    where TUow : class, IBatUnitOfWork
    where TDbContext : DbContext, IBatDbContext
{
    IBehaviorUowBuilder<TUow, TDbContext> SetAsyncSaveChange(bool isSuccess);
    IBehaviorUowBuilder<TUow, TDbContext> SetTransaction();
    IBehaviorUowBuilder<TUow, TDbContext> SetRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression, List<TEntity> entities) where TEntity : class, IBaseEntity;
    IBehaviorUowBuilder<TUow, TDbContext> SetRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression) where TEntity : class, IBaseEntity;
    IBehaviorUowBuilder<TUow, TDbContext> SetChangeTrackerEntries<TEntity>(List<TEntity> trackedEntities = null) where TEntity : class, IBaseEntity;
    IBehaviorUowBuilder<TUow, TDbContext> VerifyCall(Expression<Action<TUow>> expression, Times callTimes);
    IBehaviorUowBuilder<TUow, TDbContext> VerifyCall<TResult>(Expression<Func<TUow, TResult>> expression, Times callTimes);
    IBehaviorUowBuilder<TUow, TDbContext> VerifyTransactionCommitCall(Times times);
    IBehaviorUowBuilder<TUow, TDbContext> VerifyTransactionCommitAsyncCall(Times times);
    IBehaviorUowBuilder<TUow, TDbContext> VerifyTransactionRollBackCall(Times times);
    IBehaviorUowBuilder<TUow, TDbContext> VerifyTransactionRollBackAsyncCall(Times times);
    MockRepoBuilder<TDbContext, TEntity> Repo<TEntity>() where TEntity : class, IBaseEntity;

    Mock<TUow> AsMock();

    TUow Build();

    Lazy<TUow> BuildLazy();
}