using System.Collections.Concurrent;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Core.Entities;
using PagueVeloz.Infrastructure.Locks;

namespace PagueVeloz.Infrastructure.Repositories;

public class InMemoryAccountRepository : IAccountRepository
{
    private static readonly ConcurrentDictionary<Guid, Account> _accounts = new();
    private readonly AccountLockManager _lockManager;

    public InMemoryAccountRepository(AccountLockManager lockManager)
    {
        _lockManager = lockManager;
    }

    public async Task<Account?> GetByIdAsync(Guid accountId)
    {
        _accounts.TryGetValue(accountId, out var account);
        return await Task.FromResult(account);
    }

    public async Task UpdateAsync(Account account)
    {
        using (await _lockManager.AcquireAsync(account.Id))
        {
            _accounts[account.Id] = account;
        }
    }

    // helper para testes / seed
    public void Add(Account account)
    {
        _accounts[account.Id] = account;
    }
}
