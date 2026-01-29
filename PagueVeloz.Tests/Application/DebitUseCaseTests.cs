using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Infrastructure.Locks;
using PagueVeloz.Infrastructure.Messaging;
using PagueVeloz.Infrastructure.Repositories;
using PagueVeloz.Core.Entities;
using Xunit;

namespace PagueVeloz.Tests.Application;

public class DebitUseCaseTests
{
    [Fact]
    public async Task Debit_Should_Be_Idempotent()
    {
        var lockManager = new AccountLockManager();
        var accountRepo = new InMemoryAccountRepository(lockManager);
        var operationRepo = new InMemoryOperationRepository();
        var eventPublisher = new InMemoryEventPublisher();

        var account = new Account(Guid.NewGuid(), 500);
        account.Credit(300);
        accountRepo.Add(account);

        var useCase = new DebitUseCase(accountRepo, operationRepo, eventPublisher, lockManager);

        var operationId = Guid.NewGuid();

        var request = new DebitRequest(
            account.Id,
            operationId,
            200
        );

        await useCase.ExecuteAsync(request);
        await useCase.ExecuteAsync(request); 

        var updatedAccount = await accountRepo.GetByIdAsync(account.Id);

        Assert.Equal(100, updatedAccount!.Balance);
    }
}
