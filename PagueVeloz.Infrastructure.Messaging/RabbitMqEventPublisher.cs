using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using System.Threading;
using PagueVeloz.Application.Interfaces;

namespace PagueVeloz.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly string _exchange;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _disposed;

    public RabbitMqEventPublisher(string hostName = "localhost", string exchange = "pagueveloz.operations", string user = "guest", string password = "guest")
    {
        _exchange = exchange;
        _factory = new ConnectionFactory()
        {
            HostName = hostName,
            UserName = user,
            Password = password,
            DispatchConsumersAsync = true
        };

        // Não criar conexão aqui — inicialização será lazy em PublishAsync
    }

    public async Task PublishAsync<T>(T @event)
    {
        ThrowIfDisposed();
        await EnsureConnectedAsync();

        var payload = JsonSerializer.SerializeToUtf8Bytes(@event);
        var props = _channel!.CreateBasicProperties();
        props.Persistent = true;
        props.Headers = props.Headers ?? new Dictionary<string, object>();

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
                // BasicPublish é síncrono no cliente oficial
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
                // tenta reestabelecer conexão antes da próxima tentativa
                await RecreateConnectionAsync();
            }
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (_channel is not null && _channel.IsOpen)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_channel is not null && _channel.IsOpen)
                return;

            // Limpa conexões antigas
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }

            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task RecreateConnectionAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }

            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RabbitMqEventPublisher));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        _initLock.Dispose();
    }
}