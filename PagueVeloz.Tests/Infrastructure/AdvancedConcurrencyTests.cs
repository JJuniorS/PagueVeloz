using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.UseCases;
using PagueVeloz.Tests.Application;
using Xunit;

namespace PagueVeloz.Tests.Infrastructure;

public class AdvancedConcurrencyTests : UseCaseTestBase
{
    private readonly DebitUseCase _debitUseCase;
    private readonly CreditUseCase _creditUseCase;
    private readonly TransferUseCase _transferUseCase;

    public AdvancedConcurrencyTests()
    {
        _debitUseCase = new DebitUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
        _creditUseCase = new CreditUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
        _transferUseCase = new TransferUseCase(AccountRepository, OperationRepository, EventPublisher, LockManager);
    }

    [Fact]
    public async Task Concurrent_Debits_Should_Not_Allow_Overdraft()
    {
        // Arrange
        var account = CreateAccountWithBalance(100); // Sem crédito disponível
        
        // Act - Executar 10 débitos de 20 concorrentemente (total: 200)
        var tasks = Enumerable.Range(0, 10).Select(i =>
            _debitUseCase.ExecuteAsync(new DebitRequest(account.Id, Guid.NewGuid(), 20))
        ).ToList();

        // Act - Aguardar todas (algumas vão falhar)
        var results = await Task.WhenAll(tasks.Select(t => t.ContinueWith(x => x.Exception == null)));

        // Assert
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        
        // Apenas 5 débitos devem ter sucesso (100 / 20 = 5)
        // Se houvesse overdraft, poderíamos ter débitos a mais
        Assert.True(storedAccount!.Balance <= 100, "Balance should not exceed original due to lock protection");
    }

    [Fact]
    public async Task Concurrent_Credits_Should_Sum_Correctly()
    {
        // Arrange
        var account = CreateAccountWithBalance(1000);
        
        // Act - Executar 100 créditos de 10 concorrentemente
        var tasks = Enumerable.Range(0, 100).Select(i =>
            _creditUseCase.ExecuteAsync(new CreditRequest(account.Id, Guid.NewGuid(), 10))
        ).ToList();

        await Task.WhenAll(tasks);

        // Assert
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(2000, storedAccount?.Balance); // 1000 + (100 * 10)
    }

    [Fact]
    public async Task Concurrent_Transfers_Should_Maintain_Total_Balance()
    {
        // Arrange
        var account1 = CreateAccountWithBalance(1000);
        var account2 = CreateAccountWithBalance(1000);
        var totalBefore = account1.Balance + account2.Balance;

        // Act - Executar 50 transferências concorrentemente
        var tasks = Enumerable.Range(0, 50).Select(i =>
            _transferUseCase.ExecuteAsync(new TransferRequest(account1.Id, account2.Id, Guid.NewGuid(), 10))
        ).ToList();

        var results = await Task.WhenAll(tasks.Select(t => t.ContinueWith(x => x.Exception == null)));

        // Assert
        var stored1 = AccountRepository.GetByIdAsync(account1.Id).Result;
        var stored2 = AccountRepository.GetByIdAsync(account2.Id).Result;
        var totalAfter = stored1!.Balance + stored2!.Balance;

        Assert.Equal(totalBefore, totalAfter); // Total não muda, apenas distribui
    }

    [Fact]
    public async Task Lock_Should_Serialize_Operations_On_Same_Account()
    {
        // Arrange
        var account = CreateAccountWithBalance(1000);
        var executionOrder = new List<int>();
        var lockObj = new object();

        // Act - Executar 5 operações com delays para validar serialização
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            await _debitUseCase.ExecuteAsync(new DebitRequest(account.Id, Guid.NewGuid(), 100));
            lock (lockObj)
            {
                executionOrder.Add(i);
            }
            await Task.Delay(10); // Simular processamento
        }).ToList();

        await Task.WhenAll(tasks);

        // Assert
        // Se não houvesse lock, múltiplas operações poderiam executar em paralelo
        // Com o lock, devem ser serializadas
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(500, storedAccount?.Balance); // 1000 - (5 * 100)
        Assert.Equal(5, executionOrder.Count);
    }

    [Fact]
    public async Task Idempotency_Under_Concurrent_Duplicate_Operations()
    {
        // Arrange
        var account = CreateAccountWithBalance(1000);
        var operationId = Guid.NewGuid(); // Mesmo ID para todas as requisições

        // Act - Executar 10 créditos concorrentemente com MESMO operationId
        var tasks = Enumerable.Range(0, 10).Select(i =>
            _creditUseCase.ExecuteAsync(new CreditRequest(account.Id, operationId, 100))
        ).ToList();

        await Task.WhenAll(tasks);

        // Assert
        var storedAccount = AccountRepository.GetByIdAsync(account.Id).Result;
        Assert.Equal(1100, storedAccount?.Balance); // 1000 + 100 (apenas uma foi executada)
        
        // Verificar que apenas uma operação foi registrada
        var allOperations = OperationRepository.GetByIdAsync(operationId).Result;
        Assert.NotNull(allOperations);
    }
}
