using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.UseCases;
using Xunit;

namespace PagueVeloz.Tests.Application;

public class ReserveUseCaseTests : UseCaseTestBase
{
    private readonly ReserveUseCase _useCase;

    public ReserveUseCaseTests()
    {
        _useCase = new ReserveUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
    }

    [Fact]
    public async Task Reserve_Should_Freeze_Balance()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        var request = new ReserveRequest(account.Id, operationId, 200);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(500, storedAccount?.Balance);
        Assert.Equal(200, storedAccount?.ReservedBalance);
        AssertEventWasPublished<OperationCreatedEvent>();
    }

    [Fact]
    public async Task Reserve_Should_Fail_With_Insufficient_Balance()
    {
        // Arrange
        // Balance: 100, CreditLimit: 1000, Available: 1100
        // Reservar 1200 (mais que o disponível)
        var account = CreateAccountWithBalance(100);
        var operationId = Guid.NewGuid();
        var request = new ReserveRequest(account.Id, operationId, 1200);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Reserve_Should_Be_Idempotent()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        var request = new ReserveRequest(account.Id, operationId, 200);

        // Act
        await _useCase.ExecuteAsync(request);
        var reservedAfterFirst = AccountRepository.GetByIdAsync(account.Id).Result?.ReservedBalance;

        await _useCase.ExecuteAsync(request);
        var reservedAfterSecond = AccountRepository.GetByIdAsync(account.Id).Result?.ReservedBalance;

        // Assert
        Assert.Equal(200, reservedAfterFirst);
        Assert.Equal(200, reservedAfterSecond);
    }

    [Fact]
    public async Task Reserve_With_Credit_Limit_Should_Succeed()
    {
        // Arrange - Balance: 100, CreditLimit: 500, pode reservar até 600
        var account = CreateAccountWithBalance(100, creditLimit: 500);
        var operationId = Guid.NewGuid();
        var request = new ReserveRequest(account.Id, operationId, 300);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(300, storedAccount?.ReservedBalance);
    }
}
