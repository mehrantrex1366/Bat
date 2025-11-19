namespace Bat.Test;

public class MockBuilder<TClass> where TClass : class
{
    private readonly Mock<TClass> _mock;
    protected Mock<TClass> Mock => AsMock();

    public MockBuilder()
    {
        _mock = new Mock<TClass>();
    }

    public MockBuilder(Mock mock)
    {
        _mock = new Mock<TClass>(mock.Object);
    }


    public MockBuilder<TClass> Setup<TResult>(Expression<Func<TClass, Task<TResult>>> expression, TResult returnsResult)
    {
        _mock.Setup(expression).ReturnsAsync(returnsResult);

        return this;
    }

    public MockBuilder<TClass> Setup<TResult>(Expression<Func<TClass, TResult>> expression, TResult returnsResult) where TResult : class
    {
        _mock.Setup(expression).Returns(returnsResult);

        return this;
    }

    public MockBuilder<TClass> ThrowException<TResult>(Expression<Func<TClass, Task<TResult>>> expression, Exception exception)
    {
        _mock.Setup(expression).Throws(exception);

        return this;
    }


    public MockBuilder<TClass> SetVerifiable(Expression<Func<TClass, Task>> expression)
    {
        _mock.Setup(expression).Verifiable();

        return this;
    }

    public MockBuilder<TClass> SetVerifiable(Expression<Action<TClass>> expression)
    {
        _mock.Setup(expression).Verifiable();

        return this;
    }


    public MockBuilder<TClass> VerifyCall(Expression<Action<TClass>> expression, Times callTimes)
    {
        _mock.Verify(expression, callTimes);

        return this;
    }

    public MockBuilder<TClass> VerifyCall<TResult>(Expression<Func<TClass, TResult>> expression, Times callTimes)
    {
        _mock.Verify(expression, callTimes);

        return this;
    }


    public TClass Build() => _mock.Object;

    public Lazy<TClass> BuildLazy() => new(() => _mock.Object);

    public Mock<TClass> AsMock() => _mock;
}