namespace Bat.Test;

public class ServiceBuilder<T> where T : IScopedInjection
{
    private readonly IFixture _fixture;


    public ServiceBuilder()
        => _fixture = new Fixture().Customize(new AutoMoqCustomization());


    public ServiceBuilder<T> Inject<TMock>(TMock mock) where TMock : class
    {
        _fixture.Inject(mock);

        return this;
    }

    public T Build() => _fixture.Create<T>();
}