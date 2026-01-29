using PagueVeloz.Core.Entities;

namespace PagueVeloz.Application.Interfaces;

public interface IOperationRepository
{
    Task<Operation?> GetByIdAsync(Guid operationId);
    Task AddAsync(Operation operation);
    Task UpdateAsync(Operation operation);
}
