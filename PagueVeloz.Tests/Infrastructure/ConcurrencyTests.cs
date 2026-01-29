using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Infrastructure.Locks;
using PagueVeloz.Infrastructure.Messaging;
using PagueVeloz.Infrastructure.Repositories;
using PagueVeloz.Core.Entities;
using Xunit;

namespace PagueVeloz.Tests.Infrastructure;

public class ConcurrencyTests
{
    [Fact]
    public async Task Concurrent_Debits_Should_Not_Corrupt_Balance()
    {
        var lockManager = new AccountLockManager();
        var accountRepo = new InMemoryAccountRepository(lockManager);
        var operationRepo = new InMemoryOperationRepository();
        var eventPublisher = new InMemoryEventPublisher();

        var account = new Account(Guid.NewGuid(), 0);
        account.Credit(500);
        accountRepo.Add(account);

        var useCase = new DebitUseCase(accountRepo, operationRepo, eventPublisher);

        var tasks = Enumerable.Range(0, 5).Select(i =>
            useCase.ExecuteAsync(new DebitRequest(
                account.Id,
                Guid.NewGuid(),
                200
            ))
        );

        await Task.WhenAll(tasks);

        var updated = await accountRepo.GetByIdAsync(account.Id);

        Assert.True(updated!.Balance >= 0);
    }
}
