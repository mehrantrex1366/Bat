namespace Bat.Queue;

public class RabbitConsumer(IRabbitService rabbitService) : IRabbitConsumer, IAsyncDisposable
{
    private IChannel _channel;
    private IConnection _connection;
    private string _queueName = "Bat_Queue";
    private string _exchangeName = "Bat_Exchange";
    private readonly IRabbitService _rabbitService = rabbitService;

    public async Task Subscribe(Action<string, object> receiveEventAction, string queueName = null, string exchangeName = null,
        string routingKey = "", RabbitExchangeType exchangeType = RabbitExchangeType.Direct, bool durable = true,
        bool autoDelete = false, string consumerTag = "", IDictionary<string, object> arguments = null)
    {
        _connection = await _rabbitService.CreateConnection();
        _channel = await _connection.CreateChannelAsync();

        if (!string.IsNullOrWhiteSpace(queueName)) _queueName = $"{queueName}_Queue";
        if (!string.IsNullOrWhiteSpace(exchangeName)) _exchangeName = $"{exchangeName}_Exchange";

        await _channel.QueueDeclareAsync(_queueName, durable: durable, exclusive: false, autoDelete: autoDelete);

        if (exchangeType == RabbitExchangeType.Direct)
            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Direct, durable: durable, autoDelete: autoDelete);
        else if (exchangeType == RabbitExchangeType.Fanout)
            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Fanout, durable: durable, autoDelete: autoDelete);
        else if (exchangeType == RabbitExchangeType.Headers)
            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Headers, durable: durable, autoDelete: autoDelete);
        else if (exchangeType == RabbitExchangeType.Topic)
            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic, durable: durable, autoDelete: autoDelete);

        await _channel.QueueBindAsync(_queueName, _exchangeName, routingKey);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                await Task.Run(() => receiveEventAction.Invoke(Encoding.UTF8.GetString(eventArgs.Body.Span), eventArgs));
            };

        await _channel.BasicConsumeAsync(
                consumer: consumer,
                queue: _queueName,
                autoAck: true,
                consumerTag: consumerTag,
                arguments: arguments);
        await Task.CompletedTask;
    }

    public async Task Subscribe(IConnection connection, Action<string, object> receiveEventAction, string queueName = null, string exchangeName = null,
        string routingKey = "", RabbitExchangeType exchangeType = RabbitExchangeType.Direct, bool durable = true,
        bool autoDelete = false, string consumerTag = "", IDictionary<string, object> arguments = null)
    {
        _connection = connection;
        _channel = await connection.CreateChannelAsync();

        if (!string.IsNullOrWhiteSpace(queueName)) _queueName = $"{queueName}_Queue";
        if (!string.IsNullOrWhiteSpace(exchangeName)) _exchangeName = $"{exchangeName}_Exchange";

        await _channel.QueueDeclareAsync(_queueName, durable: durable, exclusive: false, autoDelete: autoDelete);

        if (exchangeType == RabbitExchangeType.Direct)
            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Direct, durable: durable, autoDelete: autoDelete);
        else if (exchangeType == RabbitExchangeType.Fanout)
            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Fanout, durable: durable, autoDelete: autoDelete);
        else if (exchangeType == RabbitExchangeType.Headers)
            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Headers, durable: durable, autoDelete: autoDelete);
        else if (exchangeType == RabbitExchangeType.Topic)
            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic, durable: durable, autoDelete: autoDelete);

        await _channel.QueueBindAsync(_queueName, _exchangeName, routingKey);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, eventArgs) =>
        {
            await Task.Run(() => receiveEventAction.Invoke(Encoding.UTF8.GetString(eventArgs.Body.Span), eventArgs));
        };

        await _channel.BasicConsumeAsync(
              consumer: consumer,
              queue: _queueName,
              autoAck: true,
              consumerTag: consumerTag,
              arguments: arguments);
        await Task.CompletedTask;
    }

    // Null-safe: DisposeAsync used to throw NullReferenceException if Subscribe was never called.
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

        GC.SuppressFinalize(this);
    }
}