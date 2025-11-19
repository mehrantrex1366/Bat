namespace Bat.Test;

public class NSubstituteDbBuilder<T> where T : BatDbContext
{
    private readonly T _substitute;

    public NSubstituteDbBuilder()
    {
        _substitute = Substitute.For<T>();
    }


    public NSubstituteDbBuilder<T> Setup<TEntity>(Func<T, DbSet<TEntity>> dbSetExpression, List<TEntity> entities) where TEntity : class
    {
        var mockDbSet = entities.AsQueryable().BuildMockDbSet();
        dbSetExpression(_substitute).Returns(mockDbSet.Object);

        return this;

    }

    public NSubstituteDbBuilder<T> Setup<TResult>(Func<T, TResult> methodCall, TResult returnsResult)
    {
        methodCall(_substitute).Returns(returnsResult);

        return this;
    }

    public NSubstituteDbBuilder<T> Setup<TResult>(Func<T, Task<TResult>> methodCall, TResult returnsResult)
    {
        methodCall(_substitute).Returns(Task.FromResult(returnsResult));

        return this;
    }

    public NSubstituteDbBuilder<T> WithSaveChange(bool isSuccess)
        => Setup(x => x.BatSaveChangesAsync(default), new SaveChangeResult { IsSuccess = isSuccess });

    public NSubstituteDbBuilder<T> WithDbSet<TEntity>(Func<T, DbSet<TEntity>> dbSetExpression, List<TEntity> entities) where TEntity : class
        => Setup(dbSetExpression, entities);


    public Lazy<T> Lazy() => new Lazy<T>(() => _substitute);
    public T Build() => _substitute;
}