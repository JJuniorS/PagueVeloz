using PagueVeloz.Application.Interfaces;

namespace PagueVeloz.Infrastructure.Messaging;

public class InMemoryEventPublisher : IEventPublisher
{
    private static readonly Random _random = new();

    public async Task PublishAsync<T>(T @event)
    {
        const int maxRetries = 3;
        var delay = 200;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Simula falha aleatória
                if (_random.Next(1, 4) == 1)
                    throw new Exception("Simulated event bus failure");

                Console.WriteLine($"Event published: {@event}");
                return;
            }
            catch
            {
                if (attempt == maxRetries)
                    throw;

                await Task.Delay(delay);
                delay *= 2; // backoff exponencial
            }
        }
    }
}
