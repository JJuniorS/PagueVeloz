using Microsoft.EntityFrameworkCore;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Core.Entities;
using PagueVeloz.Core.Enums;
using PagueVeloz.Infrastructure.Persistence;
using PagueVeloz.Infrastructure.Persistence.Entities;

namespace PagueVeloz.Infrastructure.Repositories;

public class EfCoreOperationRepository : IOperationRepository
{
    private readonly PagueVelozDbContext _context;

    public EfCoreOperationRepository(PagueVelozDbContext context)
    {
        _context = context;
    }

    public async Task<Operation?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Operations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        return entity != null ? MapToOperation(entity) : null;
    }

    public async Task<IEnumerable<Operation>> GetByAccountIdAsync(Guid accountId)
    {
        var entities = await _context.Operations
            .AsNoTracking()
            .Where(o => o.AccountId == accountId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return entities.Select(MapToOperation);
    }

    public async Task AddAsync(Operation operation)
    {
        var entity = MapToEntity(operation);
        await _context.Operations.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Operation operation)
    {
        // Detach any tracked entity with same key to avoid conflict
        var trackedEntity = _context.Operations.Local.FirstOrDefault(o => o.Id == operation.Id);
        if (trackedEntity != null)
        {
            _context.Entry(trackedEntity).State = EntityState.Detached;
        }

        var entity = MapToEntity(operation);
        _context.Operations.Update(entity);
        await _context.SaveChangesAsync();
    }

    private Operation MapToOperation(OperationEntity entity)
    {
        var type = Enum.Parse<EOperationType>(entity.Type);
        var operation = new Operation(entity.Id, entity.AccountId, type, entity.Amount);
        
        // Restore operation status
        var operationType = typeof(Operation);
        operationType.GetProperty("Status")?.SetValue(operation, Enum.Parse<EOperationStatus>(entity.Status));
        operationType.GetProperty("CreatedAt")?.SetValue(operation, entity.CreatedAt);
        operationType.GetProperty("CompletedAt")?.SetValue(operation, entity.CompletedAt);
        
        return operation;
    }

    private OperationEntity MapToEntity(Operation operation)
    {
        return new OperationEntity
        {
            Id = operation.Id,
            AccountId = operation.AccountId,
            Type = operation.Type.ToString(),
            Status = operation.Status.ToString(),
            Amount = operation.Amount,
            CreatedAt = operation.CreatedAt,
            CompletedAt = operation.CompletedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }
}


