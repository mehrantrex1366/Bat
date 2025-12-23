namespace Bat.Queue;

public interface IRabbitProducer : IAsyncDisposable
{
    Task<bool> Publish<T>(T message, string exchangeName = null, string routingKey = "", bool mandatory = false, IDictionary<string, object> headers = null);
}