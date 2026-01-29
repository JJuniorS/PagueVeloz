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

        var useCase = new DebitUseCase(accountRepo, operationRepo, eventPublisher, lockManager);

        // Executa 5 tentativas concorrentes; cada tarefa captura InvalidOperationException
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            try
            {
                await useCase.ExecuteAsync(new DebitRequest(
                    account.Id,
                    Guid.NewGuid(),
                    200
                ));
                return true; // sucesso
            }
            catch (InvalidOperationException)
            {
                // Falha esperada quando não há fundos suficientes -> trata como tentativa sem sucesso
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);

        var updated = await accountRepo.GetByIdAsync(account.Id);

        // Saldo não deve ficar negativo
        Assert.True(updated!.Balance >= 0);

        // Opcional: verificar que o número de débitos aplicados não excede o disponível
        var successful = results.Count(r => r);
        var maxPossible = (int)(500 / 200); // saldo inicial / valor por débito
        Assert.True(successful <= maxPossible);
    }
}
