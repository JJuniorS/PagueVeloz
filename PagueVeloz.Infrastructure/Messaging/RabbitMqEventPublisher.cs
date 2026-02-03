using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using PagueVeloz.Application.Interfaces;

namespace PagueVeloz.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IModel _channel;
    private readonly string _exchange;
    private bool _disposed;

    public RabbitMqEventPublisher(IConnection connection, string exchange = "pagueveloz.operations")
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrWhiteSpace(exchange)) throw new ArgumentNullException(nameof(exchange));

        _exchange = exchange;
        _channel = connection.CreateModel();
        _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true, autoDelete: false);
    }

    public async Task PublishAsync<T>(T @event)
    {
        if (@event is null) throw new ArgumentNullException(nameof(@event));

        var payload = JsonSerializer.SerializeToUtf8Bytes(@event);
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.Headers ??= new Dictionary<string, object>();

        // Tenta anexar operation-id se existir propriedade Id ou OperationId no evento
        var idProp = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("OperationId");
        if (idProp is not null)
        {
            var val = idProp.GetValue(@event)?.ToString();
            if (!string.IsNullOrEmpty(val))
                props.Headers["operation-id"] = Encoding.UTF8.GetBytes(val);
        }

        var routingKey = typeof(T).Name;
        const int maxAttempts = 3;
        int attempt = 0;
        int delayMs = 200;

        while (true)
        {
            try
            {
                _channel.BasicPublish(_exchange, routingKey, props, payload);
                return;
            }
            catch
            {
                attempt++;
                if (attempt >= maxAttempts)
                    throw;
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            _channel?.Close();
            _channel?.Dispose();
        }
        catch { }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
