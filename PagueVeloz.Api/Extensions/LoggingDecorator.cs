using System.Diagnostics;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Api.Logging;
using PagueVeloz.Api.Metrics;

namespace PagueVeloz.Api.Extensions;

/// <summary>
/// Decorador para adicionar logging automático em Use Cases.
/// Padrão Decorator para separar cross-cutting concerns (logging) da lógica principal.
/// </summary>
public class LoggingDecorator
{
    private readonly IOperationLogger _operationLogger;
    private readonly IPerformanceMetrics _metrics;

    public LoggingDecorator(IOperationLogger operationLogger, IPerformanceMetrics metrics)
    {
        _operationLogger = operationLogger;
        _metrics = metrics;
    }

    public async Task ExecuteWithLoggingAsync<TRequest>(
        Guid accountId,
        string operationType,
        decimal amount,
        TRequest request,
        Func<TRequest, Task> executeFunc) where TRequest : class
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _operationLogger.LogOperationStarted(accountId, operationType, amount);

            await executeFunc(request);

            stopwatch.Stop();
            _operationLogger.LogOperationCompleted(accountId, operationType, amount, stopwatch.ElapsedMilliseconds);
            _metrics.RecordMetric(
                $"operation.{operationType.ToLowerInvariant()}.duration",
                stopwatch.ElapsedMilliseconds,
                new Dictionary<string, string> { { "account_id", accountId.ToString() } }
            );
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            _operationLogger.LogOperationFailed(accountId, operationType, ex.Message, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
