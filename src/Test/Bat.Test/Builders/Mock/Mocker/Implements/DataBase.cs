namespace Bat.Test;

public class DataBase<TDbContext> : IInitialDbBuilder<TDbContext>, IBehaviorDbBuilder<TDbContext>
    where TDbContext : BatDbContext
{
    private Mock<TDbContext> _dbMock;


    public IBehaviorDbBuilder<TDbContext> Mock()
    {
        _dbMock = new Mock<TDbContext>();

        return this;
    }

    public IBehaviorDbBuilder<TDbContext> MockWithDbSet<TEntity>(
        Expression<Func<TDbContext, DbSet<TEntity>>> expression, List<TEntity> entities)
        where TEntity : class, IBaseEntity
    {
        Mock();

        _dbMock
            .Setup(expression)
            .Returns(entities.BuildMockDbSet().Object);

        return this;
    }

    public IBehaviorDbBuilder<TDbContext> WithSaveChanges(bool isSuccess)
    {
        _dbMock
            .Setup(x => x.BatSaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaveChangeResult { IsSuccess = isSuccess });

        return this;
    }

    public TDbContext Build()
    {
        if (_dbMock == null)
            throw new InvalidOperationException("Call Mock() OR MockWithDbSet() before Build().");

        return _dbMock.Object;
    }
}