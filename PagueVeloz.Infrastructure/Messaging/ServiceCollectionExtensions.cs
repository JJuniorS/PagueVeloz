using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using PagueVeloz.Application.Interfaces;

namespace PagueVeloz.Infrastructure.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RabbitMq");
        var host = section["Host"] ?? "localhost";
        var user = section["User"] ?? "guest";
        var pass = section["Password"] ?? "guest";
        var portStr = section["Port"] ?? "5672";
        var port = int.TryParse(portStr, out var p) ? p : 5672;
        var virtualHost = section["VirtualHost"] ?? "/";
        var exchange = section["Exchange"] ?? "pagueveloz.operations";

        // IConnection como singleton (1 conexão por aplicação)
        services.AddSingleton<IConnection>(sp =>
            RabbitMqConnectionFactory.CreateConnection(host, user, pass, virtualHost, port)
        );

        // IEventPublisher como singleton
        services.AddSingleton<IEventPublisher>(sp =>
            new RabbitMqEventPublisher(sp.GetRequiredService<IConnection>(), exchange)
        );

        return services;
    }
}