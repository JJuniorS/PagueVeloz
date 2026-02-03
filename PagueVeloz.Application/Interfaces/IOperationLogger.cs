namespace PagueVeloz.Application.Interfaces;

/// <summary>
/// Interface para logging de operações financeiras.
/// Abstrai a implementação de logging do Application.
/// </summary>
public interface IOperationLogger
{
    void LogOperationStarted(Guid accountId, string operationType, decimal amount);
    void LogOperationCompleted(Guid accountId, string operationType, decimal amount, long elapsedMs);
    void LogOperationFailed(Guid accountId, string operationType, string error, long elapsedMs);
    void LogIdempotencyDetected(Guid operationId);
}
