namespace Bat.Queue;

/// <remarks>
/// Fixes compared to the previous version:
/// <list type="bullet">
/// <item>every Publish opened a NEW connection and channel and overwrote the fields, so all but the last were
/// leaked (RabbitMQ connections are expensive: a TCP connection plus server-side resources each). The connection
/// and channel are now created once and reused; publishing is serialized because a channel must not be used
/// concurrently;</item>
/// <item>the exchange name was stored in a field, so a call without exchangeName published to the exchange of the
/// previous call; it is now per call (default "Bat_Exchange");</item>
/// <item>the overload that takes a caller-owned connection leaked one channel per call and DisposeAsync then closed
/// the caller's connection; it now uses a short-lived channel and never touches the caller's connection;</item>
/// <item>DisposeAsync threw NullReferenceException when nothing had been published.</item>
/// </list>
/// Register as a singleton (or at least reuse the instance) to benefit from the shared connection.
/// </remarks>
public class RabbitProducer(IRabbitService rabbitService) : IRabbitProducer, IAsyncDisposable
{
    private const string DefaultExchangeName = "Bat_Exchange";

    private IChannel _channel;
    private IConnection _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IRabbitService _rabbitService = rabbitService;

    private static string GetExchangeName(string exchangeName)
        => string.IsNullOrWhiteSpace(exchangeName) ? DefaultExchangeName : $"{exchangeName}_Exchange";

    private static BasicProperties CreateProperties(IDictionary<string, object> headers)
        => new()
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            Headers = headers
        };

    public async Task<bool> Publish<T>(T message, string exchangeName = null, string routingKey = "",
        bool mandatory = false, IDictionary<string, object> headers = null)
    {
        var body = message.SerializeToJsonUtf8Bytes();

        await _lock.WaitAsync();
        try
        {
            if (_connection is null || !_connection.IsOpen)
            {
                if (_connection is not null) await _connection.DisposeAsync();
                _connection = await _rabbitService.CreateConnection();
                _channel = null;
            }

            if (_channel is null || !_channel.IsOpen)
            {
                if (_channel is not null) await _channel.DisposeAsync();
                _channel = await _connection.CreateChannelAsync();
            }

            await _channel.BasicPublishAsync(
                    exchange: GetExchangeName(exchangeName),
                    routingKey: routingKey,
                    mandatory: mandatory,
                    basicProperties: CreateProperties(headers),
                    body: body);
        }
        finally
        {
            _lock.Release();
        }

        return true;
    }

    public async Task<bool> Publish<T, TProperties>(IConnection connection, T message, string exchangeName = null, string routingKey = "",
        bool mandatory = false, IDictionary<string, object> headers = default)
    {
        await using var channel = await connection.CreateChannelAsync();

        await channel.BasicPublishAsync(
                exchange: GetExchangeName(exchangeName),
                routingKey: routingKey,
                mandatory: mandatory,
                basicProperties: CreateProperties(headers),
                body: message.SerializeToJsonUtf8Bytes());

        return true;
    }


    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            if (_channel.IsOpen) await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            if (_connection.IsOpen) await _connection.CloseAsync();
            _connection.Dispose();
        }

        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
