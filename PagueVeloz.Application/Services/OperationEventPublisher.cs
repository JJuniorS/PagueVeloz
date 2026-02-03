using PagueVeloz.Application.Events;
using PagueVeloz.Application.Interfaces;

namespace PagueVeloz.Application.Services;

/// <summary>
/// Service centralizado para publicação de eventos de operações com retry exponencial.
/// Evita duplicação de lógica de retry nos Use Cases.
/// </summary>
public class OperationEventPublisher
{
    private readonly IEventPublisher _eventPublisher;

    public OperationEventPublisher(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Publica um evento com retry automático e backoff exponencial.
    /// </summary>
    /// <param name="event">Evento a ser publicado</param>
    /// <returns>Task completada após sucesso ou exceção se falhar após maxAttempts</returns>
    public async Task PublishWithRetryAsync<T>(T @event) where T : class
    {
        const int maxAttempts = 3;
        int attempt = 0;
        int delayMs = 200;

        while (true)
        {
            try
            {
                await _eventPublisher.PublishAsync(@event);
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
}
