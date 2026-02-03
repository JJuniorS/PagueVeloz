using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Core.Enums;
using Xunit;

namespace PagueVeloz.Tests.Application;

public class RevertUseCaseTests : UseCaseTestBase
{
    private readonly DebitUseCase _debitUseCase;
    private readonly CreditUseCase _creditUseCase;
    private readonly ReserveUseCase _reserveUseCase;
    private readonly RevertUseCase _revertUseCase;

    public RevertUseCaseTests()
    {
        _debitUseCase = new DebitUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager, OperationLoggerMock);
        _creditUseCase = new CreditUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
        _reserveUseCase = new ReserveUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
        _revertUseCase = new RevertUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
    }

    [Fact]
    public async Task Revert_Debit_Should_Credit_Back()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        
        // Fazer débito
        await _debitUseCase.ExecuteAsync(new DebitRequest(account.Id, operationId, 100));
        AssertAccountBalance(account, 400);

        // Act - Reverter
        var revertId = Guid.NewGuid();
        await _revertUseCase.ExecuteAsync(new RevertRequest(account.Id, operationId));

        // Assert
        AssertAccountBalance(account, 500); // Voltou ao valor original
        AssertEventWasPublished<OperationCreatedEvent>();
    }

    [Fact]
    public async Task Revert_Credit_Should_Debit_Back()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        
        // Fazer crédito
        await _creditUseCase.ExecuteAsync(new CreditRequest(account.Id, operationId, 100));
        AssertAccountBalance(account, 600);

        // Act - Reverter
        await _revertUseCase.ExecuteAsync(new RevertRequest(account.Id, operationId));

        // Assert
        AssertAccountBalance(account, 500); // Voltou ao valor original
    }

    [Fact]
    public async Task Revert_Reserve_Should_Release()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var operationId = Guid.NewGuid();
        
        // Fazer reserva
        await _reserveUseCase.ExecuteAsync(new ReserveRequest(account.Id, operationId, 200));
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(200, storedAccount?.ReservedBalance);

        // Act - Reverter
        await _revertUseCase.ExecuteAsync(new RevertRequest(account.Id, operationId));

        // Assert
        storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(0, storedAccount?.ReservedBalance); // Liberado
    }

    [Fact]
    public async Task Revert_Non_Existent_Operation_Should_Fail()
    {
        // Arrange
        var account = CreateAccountWithBalance(500);
        var nonExistentOperationId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _revertUseCase.ExecuteAsync(new RevertRequest(account.Id, nonExistentOperationId))
        );
    }

    [Fact]
    public async Task Revert_Non_Existent_Account_Should_Fail()
    {
        // Arrange
        var operationId = Guid.NewGuid();
        var nonExistentAccountId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _revertUseCase.ExecuteAsync(new RevertRequest(nonExistentAccountId, operationId))
        );
    }
}
