using PagueVeloz.Application.Interfaces;

namespace PagueVeloz.Api.Logging;

/// <summary>
/// Implementação de IOperationLogger para logging estruturado de operações financeiras.
/// Centraliza a lógica de logging com contexto estruturado.
/// </summary>
public class OperationLogger : IOperationLogger
{
    private readonly ILogger<OperationLogger> _logger;

    public OperationLogger(ILogger<OperationLogger> logger)
    {
        _logger = logger;
    }

    public void LogOperationStarted(Guid accountId, string operationType, decimal amount)
    {
        _logger.LogInformation(
            "Operation started: AccountId={AccountId}, Type={OperationType}, Amount={Amount}",
            accountId, operationType, amount
        );
    }

    public void LogOperationCompleted(Guid accountId, string operationType, decimal amount, long elapsedMs)
    {
        _logger.LogInformation(
            "Operation completed: AccountId={AccountId}, Type={OperationType}, Amount={Amount}, ElapsedMs={ElapsedMs}",
            accountId, operationType, amount, elapsedMs
        );
    }

    public void LogOperationFailed(Guid accountId, string operationType, string error, long elapsedMs)
    {
        _logger.LogError(
            "Operation failed: AccountId={AccountId}, Type={OperationType}, Error={Error}, ElapsedMs={ElapsedMs}",
            accountId, operationType, error, elapsedMs
        );
    }

    public void LogIdempotencyDetected(Guid operationId)
    {
        _logger.LogWarning(
            "Idempotent operation detected: OperationId={OperationId}",
            operationId
        );
    }
}

