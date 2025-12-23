using MockQueryable;

namespace Bat.Test;

public class MockDbContextBuilder<TDbContext> where TDbContext : class, IBatDbContext
{
    private readonly Mock<TDbContext> _mock;
    private Mock<IDbContextTransaction> _transactionMock;

    public MockDbContextBuilder()
    {
        _mock = new Mock<TDbContext>();
    }

    public MockDbContextBuilder(Mock<TDbContext> dbMock)
    {
        _mock = dbMock;
    }


    private MockDbContextBuilder<TDbContext> Setup<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetExpression, List<TEntity> entities)
        where TEntity : class, IBaseEntity
    {
        _mock.Setup(dbSetExpression)
            .Returns(entities.BuildMockDbSet().Object);

        return this;

    }

    private MockDbContextBuilder<TDbContext> Setup<TResult>(Expression<Func<TDbContext, Task<TResult>>> expression, TResult returnsResult)
    {
        _mock.Setup(expression)
            .ReturnsAsync(returnsResult);

        return this;
    }

    private MockDbContextBuilder<TDbContext> Setup<TResult>(Expression<Func<TDbContext, TResult>> expression, TResult returnsResult) where TResult : class
    {
        _mock.Setup(expression)
            .Returns(returnsResult);

        return this;
    }


    public MockDbContextBuilder<TDbContext> SetAsyncSaveChange(bool isSuccess)
        => Setup(x => x.BatSaveChangesAsync(It.IsAny<CancellationToken>()), new SaveChangeResult { IsSuccess = isSuccess });

    public MockDbContextBuilder<TDbContext> SetSaveChange(bool isSuccess)
        => Setup(x => x.BatSaveChanges(), new SaveChangeResult { IsSuccess = isSuccess });


    public MockDbContextBuilder<TDbContext> SetDbSet<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetExpression, List<TEntity> entities)
         where TEntity : class, IBaseEntity
        => Setup(dbSetExpression, entities);

    public MockDbContextBuilder<TDbContext> SetDbSet<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetExpression)
         where TEntity : class, IBaseEntity
        => Setup(dbSetExpression, new List<TEntity>());

    public MockDbContextBuilder<TDbContext> SetTransactionWithVerify()
    {
        var dbFacadeMock = new Mock<DatabaseFacade>(_mock.Object);
        _transactionMock = new Mock<IDbContextTransaction>();

        var dbTransaction = new MockBuilder<IDbContextTransaction>()
            .SetVerifiable(x => x.Commit())
            .SetVerifiable(x => x.Rollback())
            .SetVerifiable(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .SetVerifiable(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Build();

        var dbFacade = new MockBuilder<DatabaseFacade>(_mock)
            .Setup(x => x.BeginTransaction(), dbTransaction)
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), dbTransaction)
            .Build();

        //_mock.Setup(x => x.Database).Returns(dbFacade);

        return this;
    }

    public MockDbContextBuilder<TDbContext> SetTransaction()
    {
        _transactionMock = new Mock<IDbContextTransaction>();

        _transactionMock.Setup(x => x.Commit());
        _transactionMock.Setup(x => x.Rollback());
        _transactionMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
        _transactionMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        var databaseFacadeMock = new Mock<DatabaseFacade>(_mock.Object);

        databaseFacadeMock
            .Setup(x => x.BeginTransaction())
            .Returns(_transactionMock.Object);

        databaseFacadeMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _mock.Setup(x => x.Database)
             .Returns(databaseFacadeMock.Object);

        return this;
    }


    public MockDbContextBuilder<TDbContext> Verify<TResult>(Expression<Func<TDbContext, TResult>> expression, Times times)
    {
        _mock.Verify(expression, times);

        return this;
    }

    public MockDbContextBuilder<TDbContext> VerifyTransactionRollBackCall(Times times)
    {
        _transactionMock.Verify(x => x.Rollback(), times);

        return this;
    }

    public MockDbContextBuilder<TDbContext> VerifyTransactionRollBackAsyncCall(Times times)
    {
        _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), times);

        return this;
    }

    public MockDbContextBuilder<TDbContext> VerifyTransactionCommitCall(Times times)
    {
        _transactionMock.Verify(x => x.Commit(), times);

        return this;
    }

    public MockDbContextBuilder<TDbContext> VerifyTransactionCommitAsyncCall(Times times)
    {
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), times);

        return this;
    }


    public TDbContext Build() => _mock.Object;

    public Lazy<TDbContext> BuildLazy() => new(() => _mock.Object);

    public Mock<TDbContext> AsObject() => _mock;
}