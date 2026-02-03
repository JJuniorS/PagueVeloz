using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Core.Entities;
using Xunit;

namespace PagueVeloz.Tests.Application;

public class EdgeCaseTests : UseCaseTestBase
{
    private readonly DebitUseCase _debitUseCase;
    private readonly CreditUseCase _creditUseCase;
    private readonly ReserveUseCase _reserveUseCase;
    private readonly CaptureUseCase _captureUseCase;
    private readonly ReleaseUseCase _releaseUseCase;
    private readonly TransferUseCase _transferUseCase;

    public EdgeCaseTests()
    {
        _debitUseCase = new DebitUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager, OperationLoggerMock);
        _creditUseCase = new CreditUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
        _reserveUseCase = new ReserveUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
        _captureUseCase = new CaptureUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
        _releaseUseCase = new ReleaseUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
        _transferUseCase = new TransferUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
    }

    [Fact]
    public async Task Debit_With_Zero_Amount_Should_Fail()
    {
        // Arrange
        var account = CreateAccountWithBalance(100);
        var request = new DebitRequest(account.Id, Guid.NewGuid(), 0);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _debitUseCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Credit_With_Negative_Amount_Should_Fail()
    {
        // Arrange
        var account = CreateAccountWithBalance(100);
        var request = new CreditRequest(account.Id, Guid.NewGuid(), -50);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _creditUseCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Debit_Exact_Balance_Should_Succeed()
    {
        // Arrange
        var account = CreateAccountWithBalance(100);
        var request = new DebitRequest(account.Id, Guid.NewGuid(), 100);

        // Act
        await _debitUseCase.ExecuteAsync(request);

        // Assert
        AssertAccountBalance(account, 0);
    }

    [Fact]
    public async Task Reserve_Exact_Available_Balance_Should_Succeed()
    {
        // Arrange - AvailableAmount = Balance + CreditLimit - ReservedBalance
        var account = CreateAccountWithBalance(100, creditLimit: 400); // Available: 500
        var request = new ReserveRequest(account.Id, Guid.NewGuid(), 500);

        // Act
        await _reserveUseCase.ExecuteAsync(request);

        // Assert
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(500, storedAccount?.ReservedBalance);
    }

    [Fact]
    public async Task Capture_More_Than_Reserved_Should_Fail()
    {
        // Arrange
        var account = CreateAccountWithBalance(1000);
        var reserveId = Guid.NewGuid();
        
        // Reserve 100
        await _reserveUseCase.ExecuteAsync(new ReserveRequest(account.Id, reserveId, 100));

        // Act - Tentar capturar 200 (mais do que foi reservado)
        var captureRequest = new CaptureRequest(account.Id, Guid.NewGuid(), 200);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _captureUseCase.ExecuteAsync(captureRequest));
    }

    [Fact]
    public async Task Release_More_Than_Reserved_Should_Fail()
    {
        // Arrange
        var account = CreateAccountWithBalance(1000);
        var reserveId = Guid.NewGuid();
        
        // Reserve 100
        await _reserveUseCase.ExecuteAsync(new ReserveRequest(account.Id, reserveId, 100));

        // Act - Tentar liberar 200 (mais do que foi reservado)
        var releaseRequest = new ReleaseRequest(account.Id, Guid.NewGuid(), 200);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _releaseUseCase.ExecuteAsync(releaseRequest));
    }

    [Fact]
    public async Task Transfer_To_Same_Account_Should_Fail()
    {
        // Arrange
        var account = CreateAccountWithBalance(1000);
        var request = new TransferRequest(account.Id, account.Id, Guid.NewGuid(), 100);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _transferUseCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Multiple_Reservations_Should_Accumulate()
    {
        // Arrange
        var account = CreateAccountWithBalance(1000);

        // Act - Fazer 3 reservas
        await _reserveUseCase.ExecuteAsync(new ReserveRequest(account.Id, Guid.NewGuid(), 200));
        await _reserveUseCase.ExecuteAsync(new ReserveRequest(account.Id, Guid.NewGuid(), 300));
        await _reserveUseCase.ExecuteAsync(new ReserveRequest(account.Id, Guid.NewGuid(), 100));

        // Assert
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(600, storedAccount?.ReservedBalance); // 200 + 300 + 100
    }

    [Fact]
    public async Task Debit_After_Reserve_Should_Use_Available_Balance()
    {
        // Arrange
        var account = CreateAccountWithBalance(1000);
        var reserveId = Guid.NewGuid();
        
        // Reserve 300
        await _reserveUseCase.ExecuteAsync(new ReserveRequest(account.Id, reserveId, 300));

        // Act - Débito de 600 (disponível: 1000 - 300 = 700)
        var debitRequest = new DebitRequest(account.Id, Guid.NewGuid(), 600);
        await _debitUseCase.ExecuteAsync(debitRequest);

        // Assert
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(400, storedAccount?.Balance); // 1000 - 600
        Assert.Equal(300, storedAccount?.ReservedBalance); // Mantém
    }
}
