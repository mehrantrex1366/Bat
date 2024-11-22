namespace Bat.Queue;

public class RabbitService(IOptions<RabbitConfiguration> rabbitConfiguration) : IRabbitService
{
    private readonly RabbitConfiguration _rabbitConfiguration = rabbitConfiguration.Value;

    public async Task<IConnection> CreateConnection()
    {
        var connection = new ConnectionFactory
        {
            UserName = _rabbitConfiguration.Username,
            Password = _rabbitConfiguration.Password,
            HostName = _rabbitConfiguration.HostName
        };
        connection.Port = _rabbitConfiguration.Port > 0 ? _rabbitConfiguration.Port : connection.Port;

        return await connection.CreateConnectionAsync();
    }

    public async Task<IConnection> CreateConnection(ConnectionFactory connectionFactory)
        => await connectionFactory.CreateConnectionAsync();
}