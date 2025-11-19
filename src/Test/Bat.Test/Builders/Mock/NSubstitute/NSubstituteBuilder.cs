namespace Bat.Test;

public class NSubstituteBuilder<T> where T : class
{
    private readonly T _substitute;

    public NSubstituteBuilder()
    {
        _substitute = Substitute.For<T>();
    }

    public NSubstituteBuilder<T> Setup<TResult>(Func<T, TResult> methodCall, TResult returnsResult)
    {
        methodCall(_substitute).Returns(returnsResult);

        return this;
    }

    public NSubstituteBuilder<T> Setup<TResult>(Func<T, Task<TResult>> methodCall, TResult returnsResult)
    {
        methodCall(_substitute).Returns(Task.FromResult(returnsResult));

        return this;
    }

    public NSubstituteBuilder<T> MockDbSet<TEntity>(Func<T, DbSet<TEntity>> dbSet, List<TEntity> entities) where TEntity : class
    {
        var mockDbSet = entities.AsQueryable().BuildMockDbSet();
        dbSet(_substitute).Returns(mockDbSet.Object);

        return this;
    }


    public Lazy<T> Lazy() => new Lazy<T>(() => _substitute);

    public T Build() => _substitute;
}