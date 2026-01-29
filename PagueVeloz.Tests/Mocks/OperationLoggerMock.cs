using PagueVeloz.Application.Interfaces;

namespace PagueVeloz.Tests.Mocks;

/// <summary>
/// Mock de IOperationLogger para testes.
/// Rastreia chamadas de logging sem efetivamente logar.
/// </summary>
public class OperationLoggerMock : IOperationLogger
{
    public List<(string Type, Guid AccountId, decimal Amount)> StartedOperations { get; } = new();
    public List<(string Type, Guid AccountId, decimal Amount, long ElapsedMs)> CompletedOperations { get; } = new();
    public List<(string Type, Guid AccountId, string Error, long ElapsedMs)> FailedOperations { get; } = new();
    public List<Guid> IdempotentOperations { get; } = new();

    public void LogOperationStarted(Guid accountId, string operationType, decimal amount)
    {
        StartedOperations.Add((operationType, accountId, amount));
    }

    public void LogOperationCompleted(Guid accountId, string operationType, decimal amount, long elapsedMs)
    {
        CompletedOperations.Add((operationType, accountId, amount, elapsedMs));
    }

    public void LogOperationFailed(Guid accountId, string operationType, string error, long elapsedMs)
    {
        FailedOperations.Add((operationType, accountId, error, elapsedMs));
    }

    public void LogIdempotencyDetected(Guid operationId)
    {
        IdempotentOperations.Add(operationId);
    }

    public void Reset()
    {
        StartedOperations.Clear();
        CompletedOperations.Clear();
        FailedOperations.Clear();
        IdempotentOperations.Clear();
    }
}
