using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Core.Entities;
using Xunit;

namespace PagueVeloz.Tests.Application;

public class TransferUseCaseTests : UseCaseTestBase
{
    private readonly TransferUseCase _useCase;

    public TransferUseCaseTests()
    {
        _useCase = new TransferUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
    }

    [Fact]
    public async Task Transfer_Should_Move_Balance_Between_Accounts()
    {
        // Arrange
        var sourceAccount = CreateAccountWithBalance(500);
        var destAccount = CreateAccountWithBalance(100);
        var operationId = Guid.NewGuid();
        var request = new TransferRequest(sourceAccount.Id, destAccount.Id, operationId, 200);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        AssertAccountBalance(sourceAccount, 300); // 500 - 200
        AssertAccountBalance(destAccount, 300);   // 100 + 200
        AssertEventWasPublished<OperationCreatedEvent>();
    }

    [Fact]
    public async Task Transfer_Should_Fail_With_Insufficient_Balance()
    {
        // Arrange
        // Balance: 100, CreditLimit: 1000, Available: 1100
        // Transferir 1200 (mais que o disponível)
        var sourceAccount = CreateAccountWithBalance(100);
        var destAccount = CreateAccountWithBalance(100);
        var operationId = Guid.NewGuid();
        var request = new TransferRequest(sourceAccount.Id, destAccount.Id, operationId, 1200);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Transfer_Should_Be_Idempotent()
    {
        // Arrange
        var sourceAccount = CreateAccountWithBalance(500);
        var destAccount = CreateAccountWithBalance(100);
        var operationId = Guid.NewGuid();
        var request = new TransferRequest(sourceAccount.Id, destAccount.Id, operationId, 200);

        // Act
        await _useCase.ExecuteAsync(request);
        var sourceBalanceAfterFirst = AccountRepository.GetByIdAsync(sourceAccount.Id).Result?.Balance;
        var destBalanceAfterFirst = AccountRepository.GetByIdAsync(destAccount.Id).Result?.Balance;

        await _useCase.ExecuteAsync(request);
        var sourceBalanceAfterSecond = AccountRepository.GetByIdAsync(sourceAccount.Id).Result?.Balance;
        var destBalanceAfterSecond = AccountRepository.GetByIdAsync(destAccount.Id).Result?.Balance;

        // Assert
        Assert.Equal(sourceBalanceAfterFirst, sourceBalanceAfterSecond);
        Assert.Equal(destBalanceAfterFirst, destBalanceAfterSecond);
    }

    [Fact]
    public async Task Transfer_With_Non_Existent_Source_Should_Fail()
    {
        // Arrange
        var destAccount = CreateAccountWithBalance(100);
        var operationId = Guid.NewGuid();
        var request = new TransferRequest(Guid.NewGuid(), destAccount.Id, operationId, 100);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Transfer_With_Non_Existent_Destination_Should_Fail()
    {
        // Arrange
        var sourceAccount = CreateAccountWithBalance(100);
        var operationId = Guid.NewGuid();
        var request = new TransferRequest(sourceAccount.Id, Guid.NewGuid(), operationId, 100);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
    }
}
