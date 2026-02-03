using PagueVeloz.Application.Interfaces;

namespace PagueVeloz.Tests.Mocks;

/// <summary>
/// Mock de IEventPublisher para testes sem dependência de RabbitMQ.
/// Rastreia eventos publicados para validação.
/// </summary>
public class EventPublisherMock : IEventPublisher
{
    public List<object> PublishedEvents { get; } = new();
    public int PublishCallCount { get; private set; }
    public bool ShouldThrowException { get; set; }
    public int ThrowExceptionOnCallNumber { get; set; } = -1; // -1 = never

    public async Task PublishAsync<T>(T @event)
    {
        PublishCallCount++;

        // Simular falha em chamada específica (para testar retry)
        if (ShouldThrowException && PublishCallCount == ThrowExceptionOnCallNumber)
        {
            throw new InvalidOperationException("Simulated RabbitMQ failure");
        }

        PublishedEvents.Add(@event!);
        await Task.CompletedTask; // Simular async
    }

    public void Reset()
    {
        PublishedEvents.Clear();
        PublishCallCount = 0;
        ShouldThrowException = false;
        ThrowExceptionOnCallNumber = -1;
    }

    public T? GetLastPublishedEvent<T>() where T : class
    {
        return PublishedEvents.LastOrDefault() as T;
    }

    public IEnumerable<T> GetPublishedEvents<T>() where T : class
    {
        return PublishedEvents.OfType<T>();
    }
}
