using Microsoft.EntityFrameworkCore;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Core.Entities;
using PagueVeloz.Core.Enums;
using PagueVeloz.Infrastructure.Persistence;
using PagueVeloz.Infrastructure.Persistence.Entities;

namespace PagueVeloz.Infrastructure.Repositories;

public class EfCoreAccountRepository : IAccountRepository
{
    private readonly PagueVelozDbContext _context;

    public EfCoreAccountRepository(PagueVelozDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        return entity != null ? MapToAccount(entity) : null;
    }

    public async Task<IEnumerable<Account>> GetByClientIdAsync(Guid clientId)
    {
        var entities = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.ClientId == clientId)
            .ToListAsync();
        return entities.Select(MapToAccount);
    }

    public async Task AddAsync(Account account)
    {
        var entity = MapToEntity(account);
        await _context.Accounts.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Account account)
    {
        // Detach any tracked entity with same key to avoid conflict
        var trackedEntity = _context.Accounts.Local.FirstOrDefault(a => a.Id == account.Id);
        if (trackedEntity != null)
        {
            _context.Entry(trackedEntity).State = EntityState.Detached;
        }

        var entity = MapToEntity(account);
        _context.Accounts.Update(entity);
        await _context.SaveChangesAsync();
    }

    private Account MapToAccount(AccountEntity entity)
    {
        var account = new Account(entity.ClientId, entity.CreditLimit);
        
        // Restore state from entity
        var accountType = typeof(Account);
        accountType.GetProperty("Id")?.SetValue(account, entity.Id);
        accountType.GetProperty("Balance")?.SetValue(account, entity.Balance);
        accountType.GetProperty("ReservedBalance")?.SetValue(account, entity.ReservedBalance);
        accountType.GetProperty("Status")?.SetValue(account, Enum.Parse<EAccountStatus>(entity.Status));
        
        return account;
    }

    private AccountEntity MapToEntity(Account account)
    {
        return new AccountEntity
        {
            Id = account.Id,
            ClientId = account.ClientId,
            Balance = account.Balance,
            ReservedBalance = account.ReservedBalance,
            AvailableBalance = account.AvailableAmount(),
            CreditLimit = account.CreditLimit,
            Status = account.Status.ToString(),
            UpdatedAt = DateTime.UtcNow
        };
    }
}


