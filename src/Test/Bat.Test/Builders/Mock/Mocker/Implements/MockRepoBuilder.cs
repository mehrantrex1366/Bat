namespace Bat.Test;

public class MockRepoBuilder<TDbContext, TEntity>  : IInitialRepoBuilder<TDbContext, TEntity>, IBehaviorRepoBuilder<TDbContext, TEntity>
    where TDbContext : DbContext, IBatDbContext
    where TEntity : class, IBaseEntity
{
    private Mock<TDbContext> _dbMock;
    private Mock<EFGenericRepo<TEntity>> _repoMock;


    public MockRepoBuilder()
    {
        
    }

    public MockRepoBuilder(Mock<EFGenericRepo<TEntity>> repoMock, Mock<TDbContext> dbMock)
    {
        _repoMock = repoMock;
        _dbMock = dbMock;
    }


    public IBehaviorRepoBuilder<TDbContext, TEntity> MockWithDataBase(Mock<TDbContext> dbMock)
    {
        _dbMock = dbMock;
        _repoMock = new Mock<EFGenericRepo<TEntity>>(_dbMock.Object);

        return this;
    }

    public IBehaviorRepoBuilder<TDbContext, TEntity> Setup(Expression<Action<EFGenericRepo<TEntity>>> expression)
    {
        _repoMock.Setup(expression).Verifiable();

        return this;
    }

    public IBehaviorRepoBuilder<TDbContext, TEntity> SetupPartialUpdate()
        => Setup(x => x.PartialUpdate(It.IsAny<TEntity>(), It.IsAny<Expression<Func<TEntity, object>>[]>()));

    public IBehaviorRepoBuilder<TDbContext, TEntity> SetAsNoTracking()
    {
        _dbMock.Setup(x => x.Set<TEntity>().AsNoTracking()).Verifiable();
        //_repoMock.Setup(x => x.AsNoTracking()).Verifiable();

        return this;
    }

    public IBehaviorRepoBuilder<TDbContext, TEntity> VerifyPartialUpdate(Times callTimes)
    {
        _repoMock.Verify(x => x.PartialUpdate(It.IsAny<TEntity>(), It.IsAny<Expression<Func<TEntity, object>>[]>()), callTimes);

        return this;
    }


    public EFGenericRepo<TEntity> Build() => _repoMock.Object;

    public Mock<EFGenericRepo<TEntity>> AsMock() => _repoMock;
}