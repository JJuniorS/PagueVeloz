using System.Collections.Concurrent;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Core.Entities;
using PagueVeloz.Infrastructure.Locks;

namespace PagueVeloz.Infrastructure.Repositories;

public class InMemoryAccountRepository : IAccountRepository
{
    private static readonly ConcurrentDictionary<Guid, Account> _accounts = new();

    // Mantemos a assinatura para compatibilidade com os testes (pode ser injetado),
    // mas o repositório não adquire locks — quem gerencia a concorrência é o UseCase.
    public InMemoryAccountRepository(IAccountLockManager _)
    {
    }

    public async Task<Account?> GetByIdAsync(Guid accountId)
    {
        _accounts.TryGetValue(accountId, out var account);
        return await Task.FromResult(account);
    }

    public Task UpdateAsync(Account account)
    {
        // Removida a aquisição do lock para evitar deadlock quando o UseCase
        // já possui o lock da conta. ConcurrentDictionary é thread-safe.
        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }

    // helper para testes / seed
    public void Add(Account account)
    {
        _accounts[account.Id] = account;
    }
}
