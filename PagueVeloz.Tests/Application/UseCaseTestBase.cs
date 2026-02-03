using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.Services;
using PagueVeloz.Core.Entities;
using PagueVeloz.Infrastructure.Locks;
using PagueVeloz.Infrastructure.Repositories;
using PagueVeloz.Tests.Mocks;
using Xunit;

namespace PagueVeloz.Tests.Application;

/// <summary>
/// Classe base para testes de Use Cases com setup padrão.
/// Reduz duplicação de código entre testes.
/// </summary>
public abstract class UseCaseTestBase
{
    protected AccountLockManager LockManager { get; }
    protected InMemoryAccountRepository AccountRepository { get; }
    protected InMemoryOperationRepository OperationRepository { get; }
    protected EventPublisherMock EventPublisherMock { get; }
    protected OperationEventPublisher EventPublisher { get; }
    protected OperationLoggerMock OperationLoggerMock { get; }

    public UseCaseTestBase()
    {
        LockManager = new AccountLockManager();
        AccountRepository = new InMemoryAccountRepository(LockManager);
        OperationRepository = new InMemoryOperationRepository();
        EventPublisherMock = new EventPublisherMock();
        EventPublisher = new OperationEventPublisher(EventPublisherMock);
        OperationLoggerMock = new OperationLoggerMock();
    }

    protected Account CreateAccountWithBalance(decimal balance, decimal creditLimit = 1000)
    {
        var account = new Account(Guid.NewGuid(), creditLimit);
        if (balance > 0)
        {
            account.Credit(balance);
        }
        AccountRepository.Add(account);
        return account;
    }

    protected void AssertEventWasPublished<T>() where T : class
    {
        var @event = EventPublisherMock.GetLastPublishedEvent<T>();
        Assert.NotNull(@event);
    }

    protected void AssertNoEventWasPublished()
    {
        Assert.Empty(EventPublisherMock.PublishedEvents);
    }

    protected void AssertAccountBalance(Account account, decimal expectedBalance)
    {
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.NotNull(storedAccount);
        Assert.Equal(expectedBalance, storedAccount.Balance);
    }
}
