namespace Bat.Queue;

public interface IRabbitService
{
    Task<IConnection> CreateConnection();
    Task<IConnection> CreateConnection(ConnectionFactory connectionFactory);
}