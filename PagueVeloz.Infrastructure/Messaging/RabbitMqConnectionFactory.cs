using RabbitMQ.Client;

namespace PagueVeloz.Infrastructure.Messaging;

public static class RabbitMqConnectionFactory
{
    public static IConnection CreateConnection(
        string hostName = "localhost",
        string userName = "guest",
        string password = "guest",
        string virtualHost = "/",
        int port = 5672)
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            VirtualHost = virtualHost,
            Port = port,
            DispatchConsumersAsync = true
        };

        return factory.CreateConnection();
    }
}