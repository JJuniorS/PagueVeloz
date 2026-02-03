using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.UseCases;
using Xunit;

namespace PagueVeloz.Tests.Application;

public class DebitUseCaseTests : UseCaseTestBase
{
    private readonly DebitUseCase _useCase;

    public DebitUseCaseTests()
    {
        _useCase = new DebitUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager, OperationLoggerMock);
    }

    [Fact]
    public async Task Debit_With_Valid_Amount_Should_Succeed()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        var request = new DebitRequest(account.Id, operationId, 100);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        AssertAccountBalance(account, 400);
        AssertEventWasPublished<OperationCreatedEvent>();
    }

    [Fact]
    public async Task Debit_Should_Fail_With_Insufficient_Balance()
    {
        // Arrange
        // Balance: 50, CreditLimit: 1000, Available: 1050
        // Debitar 1100 (mais que o disponível)
        var account = CreateAccountWithBalance(50);
        var operationId = Guid.NewGuid();
        var request = new DebitRequest(account.Id, operationId, 1100);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Debit_Should_Be_Idempotent_With_Same_OperationId()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        var request = new DebitRequest(account.Id, operationId, 100);

        // Act - Primeira requisição
        await _useCase.ExecuteAsync(request);
        var balanceAfterFirst = AccountRepository.GetByIdAsync(account.Id).Result?.Balance;

        // Act - Segunda requisição com mesmo OperationId (idempotência)
        await _useCase.ExecuteAsync(request);
        var balanceAfterSecond = AccountRepository.GetByIdAsync(account.Id).Result?.Balance;

        // Assert - Saldo não deve mudar
        Assert.Equal(balanceAfterFirst, balanceAfterSecond);
        Assert.Equal(400, balanceAfterSecond);
    }

    [Fact]
    public async Task Debit_With_Credit_Limit_Should_Succeed()
    {
        // Arrange
        var account = CreateAccountWithBalance(100, creditLimit: 500);
        var operationId = Guid.NewGuid();
        var request = new DebitRequest(account.Id, operationId, 300); // Balance: 100, CreditLimit: 500, Total: 600

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        AssertAccountBalance(account, -200); // Usa crédito
        AssertEventWasPublished<OperationCreatedEvent>();
    }

    [Fact]
    public async Task Debit_Should_Publish_Event()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        var request = new DebitRequest(account.Id, operationId, 100);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        var @event = EventPublisherMock.GetLastPublishedEvent<OperationCreatedEvent>();
        Assert.NotNull(@event);
        Assert.Equal(account.Id, @event.AccountId);
        Assert.Equal(100, @event.Amount);
    }

    [Fact]
    public async Task Debit_With_Non_Existent_Account_Should_Fail()
    {
        // Arrange
        var operationId = Guid.NewGuid();
        var request = new DebitRequest(Guid.NewGuid(), operationId, 100); // Conta não existe

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Debit_Should_Create_Operation_Record()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        var request = new DebitRequest(account.Id, operationId, 100);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        var operation = OperationRepository.GetByIdAsync(operationId).Result;
        Assert.NotNull(operation);
        Assert.Equal(operationId, operation.Id);
        Assert.Equal(account.Id, operation.AccountId);
        Assert.Equal(100, operation.Amount);
    }
}
