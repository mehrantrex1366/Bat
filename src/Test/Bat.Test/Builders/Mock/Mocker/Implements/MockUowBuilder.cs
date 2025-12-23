namespace Bat.Test;

public class MockUowBuilder<TUow, TDbContext> : IInitialUowBuilder<TUow, TDbContext>, IBehaviorUowBuilder<TUow, TDbContext>
    where TUow : class, IBatUnitOfWork
    where TDbContext : DbContext, IBatDbContext
{
    private Mock<TUow> _uowMock;
    private Mock<TDbContext> _dbMock;
    private Mock<IDbContextTransaction> _transactionMock;
    private readonly List<object> _repos = new();


    private void AddRepo<TEntity>(Mock<EFGenericRepo<TEntity>> repo) where TEntity : class, IBaseEntity
        => _repos.Add(repo);


    public IBehaviorUowBuilder<TUow, TDbContext> Mock()
    {
        _dbMock = new Mock<TDbContext>();
        _uowMock = new Mock<TUow>(_dbMock.Object, It.IsAny<IServiceProvider>());

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> MockWithDbSet<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> expression, List<TEntity> entities)
        where TEntity : class, IBaseEntity
    {
        _dbMock = new Mock<TDbContext>();
        _dbMock
            .Setup(expression)
            .Returns(entities.BuildMockDbSet().Object);

        _uowMock = new Mock<TUow>(_dbMock.Object, new Mock<IServiceProvider>().Object);


        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> MockWithRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression, List<TEntity> entities)
        where TEntity : class, IBaseEntity
    {
        _dbMock = new Mock<TDbContext>();
        _dbMock
            .Setup(x => x.Set<TEntity>())
            .Returns(entities.BuildMockDbSet().Object);

        var repoMockBuilder = new MockRepoBuilder<TDbContext, TEntity>().MockWithDataBase(_dbMock);
        AddRepo(repoMockBuilder.AsMock());

        _uowMock = new Mock<TUow>(_dbMock.Object, new Mock<IServiceProvider>().Object);
        _uowMock
            .Setup(expression)
            .Returns(repoMockBuilder.Build());

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> MockWithRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression)
        where TEntity : class, IBaseEntity
    {
        _dbMock = new Mock<TDbContext>();
        _dbMock.Setup(x => x.Set<TEntity>())
           .Returns(new List<TEntity>().BuildMockDbSet().Object);

        var repoMock = new Mock<EFGenericRepo<TEntity>>(_dbMock.Object);
        _uowMock = new Mock<TUow>(_dbMock.Object, new Mock<IServiceProvider>().Object);
        _uowMock
            .Setup(expression)
            .Returns(repoMock.Object);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> MockWithReadOnlyRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression, List<TEntity> entities)
    where TEntity : class, IBaseEntity
    {
        _dbMock = new Mock<TDbContext>();
        _dbMock
            .Setup(x => x.Set<TEntity>())
            .Returns(entities.BuildMockDbSet().Object);

        var repoMock = new Mock<EFGenericRepo<TEntity>>(_dbMock.Object);

        _uowMock = new Mock<TUow>(_dbMock.Object, new Mock<IServiceProvider>().Object);
        _uowMock
            .Setup(expression)
            .Returns(repoMock.Object);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> MockWithReadOnlyRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression)
        where TEntity : class, IBaseEntity
    {
        _dbMock = new Mock<TDbContext>();
        _dbMock.Setup(x => x.Set<TEntity>())
           .Returns(new List<TEntity>().BuildMockDbSet().Object);

        var repoMock = new Mock<EFGenericRepo<TEntity>>(_dbMock.Object);
        _uowMock = new Mock<TUow>(_dbMock.Object, new Mock<IServiceProvider>().Object);
        _uowMock
            .Setup(expression)
            .Returns(repoMock.Object);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> SetRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression, List<TEntity> entities)
        where TEntity : class, IBaseEntity
    {
        _dbMock.Setup(x => x.Set<TEntity>())
            .Returns(entities.BuildMockDbSet().Object);

        var repoMockBuilder = new MockRepoBuilder<TDbContext, TEntity>().MockWithDataBase(_dbMock);
        AddRepo(repoMockBuilder.AsMock());

        _uowMock
            .Setup(expression)
            .Returns(repoMockBuilder.Build());

        return this;

    }

    public IBehaviorUowBuilder<TUow, TDbContext> SetRepo<TEntity>(Expression<Func<TUow, EFGenericRepo<TEntity>>> expression)
      where TEntity : class, IBaseEntity
    {
        _dbMock.Setup(x => x.Set<TEntity>())
            .Returns(new List<TEntity>().BuildMockDbSet().Object);

        var repoMockBuilder = new MockRepoBuilder<TDbContext, TEntity>().MockWithDataBase(_dbMock);
        AddRepo(repoMockBuilder.AsMock());

        _uowMock
            .Setup(expression)
            .Returns(repoMockBuilder.Build());

        return this;

    }

    public IBehaviorUowBuilder<TUow, TDbContext> SetTransaction()
    {
        var dbFacadeMock = new Mock<DatabaseFacade>(_dbMock.Object);
        var dbTransactionMock = new Mock<IDbContextTransaction>();

        var dbTransactionBuilder = new MockBuilder<IDbContextTransaction>()
            .SetVerifiable(x => x.Commit())
            .SetVerifiable(x => x.Rollback())
            .SetVerifiable(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .SetVerifiable(x => x.CommitAsync(It.IsAny<CancellationToken>()));

        _transactionMock = dbTransactionBuilder.AsMock();
        var dbTransaction = dbTransactionBuilder.Build();

        var dbFacade = new MockBuilder<DatabaseFacade>(_dbMock)
            .Setup(x => x.BeginTransaction(), dbTransaction)
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), dbTransaction)
            .Build();

        _uowMock.Setup(x => x.Database).Returns(dbFacade);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> SetAsyncSaveChange(bool isSuccess)
    {
        _uowMock
            .Setup(x => x.BatSaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaveChangeResult
            {
                IsSuccess = isSuccess,
                Message = isSuccess ? null : "Save Change Error",
                Exception = isSuccess ? null : new Exception("Save Change Exception")
            });

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> VerifyCall(Expression<Action<TUow>> expression, Times callTimes)
    {
        _uowMock.Verify(expression, callTimes);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> VerifyCall<TResult>(Expression<Func<TUow, TResult>> expression, Times callTimes)
    {
        _uowMock.Verify(expression, callTimes);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> VerifySaveChangeCall(Times times)
    {
        _uowMock.Verify(x => x.BatSaveChanges(), times);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> VerifyAsyncSaveChangeCall(Times times)
    {
        _uowMock.Verify(x => x.BatSaveChangesAsync(It.IsAny<CancellationToken>()), times);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> VerifyTransactionRollBackCall(Times times)
    {
        _transactionMock.Verify(x => x.Rollback(), times);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> VerifyTransactionRollBackAsyncCall(Times times)
    {
        _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), times);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> VerifyTransactionCommitCall(Times times)
    {
        _transactionMock.Verify(x => x.Commit(), times);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> VerifyTransactionCommitAsyncCall(Times times)
    {
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), times);

        return this;
    }

    public IBehaviorUowBuilder<TUow, TDbContext> SetChangeTrackerEntries<TEntity>(List<TEntity> trackedEntities = null)
        where TEntity : class, IBaseEntity
    {
        // ChangeTracker and EntityEntry are both abstract classes with internal constructors
        // so they cannot be mocked directly. The solution is to mock the virtual ChangeTracker
        // property on IssUnitOfWork to return null, which causes DetachAddedCustomer to handle
        // the null case gracefully.
        // 
        // Actually, we can use a different approach: Setup the ChangeTracker property to return
        // a value that when Entries() is called, returns an empty enumerable. But since we can't
        // mock ChangeTracker itself, we'll take advantage of the fact that UnitOfWork.ChangeTracker
        // is a virtual property - we can setup it to return null and let DetachAddedCustomer
        // handle the null reference gracefully by catching the exception.
        //
        // Wait - looking at DetachAddedCustomer, it doesn't have try-catch. Let me try a different
        // approach: Setup CallBase = true on DbContext mock and let it create a real ChangeTracker.

        // Enable CallBase to allow DbContext to create real ChangeTracker
        _dbMock.CallBase = true;

        return this;
    }

    public TUow Build()
    {
        if (_uowMock == null)
            throw new InvalidOperationException("Call Mock() before Build().");

        return _uowMock.Object;
    }

    public Lazy<TUow> BuildLazy()
    {
        if (_uowMock == null)
            throw new InvalidOperationException("Call Mock() before Build().");

        return new(() => _uowMock.Object);
    }

    public Mock<TUow> AsMock() => _uowMock;

    public MockRepoBuilder<TDbContext, TEntity> Repo<TEntity>() where TEntity : class, IBaseEntity
      => new MockRepoBuilder<TDbContext, TEntity>(_repos.OfType<Mock<EFGenericRepo<TEntity>>>().FirstOrDefault(), _dbMock);
}