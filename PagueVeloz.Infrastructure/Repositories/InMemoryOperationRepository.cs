using System.Collections.Concurrent;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Core.Entities;

namespace PagueVeloz.Infrastructure.Repositories;

public class InMemoryOperationRepository : IOperationRepository
{
    private static readonly ConcurrentDictionary<Guid, Operation> _operations = new();

    public Task<Operation?> GetByIdAsync(Guid operationId)
    {
        _operations.TryGetValue(operationId, out var operation);
        return Task.FromResult(operation);
    }

    public Task AddAsync(Operation operation)
    {
        _operations.TryAdd(operation.Id, operation);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Operation operation)
    {
        _operations[operation.Id] = operation;
        return Task.CompletedTask;
    }
}
