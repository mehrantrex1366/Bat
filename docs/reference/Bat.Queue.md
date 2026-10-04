# Bat.Queue

`src/Queue/Bat.Queue` · namespace `Bat.Queue` · depends on Bat.Core, RabbitMQ.Client 7, Experimental.System.Messaging.

## RabbitMQ
- `RabbitConfiguration { HostName, Port (0 = default), Username, Password }` via `IOptions<RabbitConfiguration>`.
- `IRabbitService` / `RabbitService.CreateConnection([ConnectionFactory])` — always a new connection (caller owns it).
- `IRabbitProducer` / `RabbitProducer`:
  - `Publish<T>(message, exchangeName = null, routingKey = "", mandatory, headers)` — JSON (UTF-8) body,
    `ContentType = application/json`; exchange = `"{exchangeName}_Exchange"` or `"Bat_Exchange"`. One connection + channel per
    producer instance, created lazily, reused, recreated if closed; publishes are serialized. **Register as singleton** (or reuse the instance).
  - `Publish<T, TProperties>(IConnection, …)` — uses the caller's connection with a short-lived channel (caller keeps ownership).
  - Exchanges are not declared by the producer — the consumer side declares them.
- `IRabbitConsumer` / `RabbitConsumer.Subscribe(Action<string, object> onMessage, queueName, exchangeName, routingKey,
  exchangeType = Direct, durable = true, autoDelete = false, consumerTag, arguments)` — declares `"{queue}_Queue"`
  and `"{exchange}_Exchange"`, binds, consumes with **autoAck = true** (messages are lost if the handler fails).
  The handler receives the UTF-8 body and the `BasicDeliverEventArgs` and runs via `Task.Run`.
  `DisposeAsync` closes the channel and the connection (also when the connection was passed in).

## MSMQ
`IMsmQueue` interface only (`Send<T>`, `Receive<T>`); Windows-only technology, no implementation in the package.
