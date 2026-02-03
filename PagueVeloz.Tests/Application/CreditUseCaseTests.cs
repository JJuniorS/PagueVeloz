using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.UseCases;
using Xunit;

namespace PagueVeloz.Tests.Application;

public class CreditUseCaseTests : UseCaseTestBase
{
    private readonly CreditUseCase _useCase;

    public CreditUseCaseTests()
    {
        _useCase = new CreditUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
    }

    [Fact]
    public async Task Credit_Should_Increase_Balance()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        var request = new CreditRequest(account.Id, operationId, 100);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        AssertAccountBalance(account, 600);
        AssertEventWasPublished<OperationCreatedEvent>();
    }

    [Fact]
    public async Task Credit_Should_Be_Idempotent()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        var request = new CreditRequest(account.Id, operationId, 100);

        // Act
        await _useCase.ExecuteAsync(request);
        var balanceAfterFirst = AccountRepository.GetByIdAsync(account.Id).Result?.Balance;

        await _useCase.ExecuteAsync(request);
        var balanceAfterSecond = AccountRepository.GetByIdAsync(account.Id).Result?.Balance;

        // Assert
        Assert.Equal(600, balanceAfterFirst);
        Assert.Equal(600, balanceAfterSecond);
    }

    [Fact]
    public async Task Credit_With_Non_Existent_Account_Should_Fail()
    {
        // Arrange
        var operationId = Guid.NewGuid();
        var request = new CreditRequest(Guid.NewGuid(), operationId, 100);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Credit_Should_Create_Operation_Record()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        var request = new CreditRequest(account.Id, operationId, 100);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        var operation = OperationRepository.GetByIdAsync(operationId).Result;
        Assert.NotNull(operation);
        Assert.Equal(100, operation.Amount);
    }
}
