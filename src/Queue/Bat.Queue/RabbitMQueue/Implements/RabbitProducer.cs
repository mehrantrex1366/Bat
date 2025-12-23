namespace Bat.Queue;

public class RabbitProducer(IRabbitService rabbitService) : IRabbitProducer, IAsyncDisposable
{
    private IChannel _channel;
    private IConnection _connection;
    private string _exchangeName = "Bat_Exchange";
    private readonly IRabbitService _rabbitService = rabbitService;

    public async Task<bool> Publish<T>(T message, string exchangeName = null, string routingKey = "",
        bool mandatory = false, IDictionary<string, object> headers = null)
    {
        _connection = await _rabbitService.CreateConnection();
        _channel = await _connection.CreateChannelAsync();

        if (!string.IsNullOrWhiteSpace(exchangeName)) _exchangeName = $"{exchangeName}_Exchange";

        var basicProps = new BasicProperties
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            Headers = headers
        };

        await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: mandatory,
                basicProperties: basicProps,
                body: Encoding.UTF8.GetBytes(message.SerializeToJson()));

        return true;
    }

    public async Task<bool> Publish<T, TProperties>(IConnection connection, T message, string exchangeName = null, string routingKey = "",
        bool mandatory = false, IDictionary<string, object> headers = default)
    {
        _connection = connection;
        _channel = await _connection.CreateChannelAsync();

        if (!string.IsNullOrWhiteSpace(exchangeName)) _exchangeName = $"{exchangeName}_Exchange";

        var basicProps = new BasicProperties
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            Headers = headers
        };

        await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: mandatory,
                basicProperties: basicProps,
                body: Encoding.UTF8.GetBytes(message.SerializeToJson()));

        return true;
    }


    public async ValueTask DisposeAsync()
    {
        if (_channel.IsOpen) await _channel.CloseAsync();
        _channel.Dispose();

        if (_connection.IsOpen) await _connection.CloseAsync();
        _connection.Dispose();

        GC.SuppressFinalize(this);
    }
}