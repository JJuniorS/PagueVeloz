using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Core.Entities;
using PagueVeloz.Core.Enums;

namespace PagueVeloz.Application.UseCases;

public class DebitUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAccountLockManager _lockManager;

    public DebitUseCase(
        IAccountRepository accountRepository,
        IOperationRepository operationRepository,
        IEventPublisher eventPublisher,
        IAccountLockManager lockManager)
    {
        _accountRepository = accountRepository;
        _operationRepository = operationRepository;
        _eventPublisher = eventPublisher;
        _lockManager = lockManager;
    }

    public async Task ExecuteAsync(DebitRequest request)
    {
        // Buscar conta (previamente)
        var account = await _accountRepository.GetByIdAsync(request.AccountId)
            ?? throw new InvalidOperationException("Account not found.");

        // Adquire lock por conta para evitar condições de corrida
        using (await _lockManager.AcquireAsync(account.Id))
        {
            // Idempotência: checar dentro do lock
            var existingOperation = await _operationRepository.GetByIdAsync(request.OperationId);
            if (existingOperation is not null)
                return;

            // Executar regra de negócio (pode lançar se sem saldo)
            account.Debit(request.Amount);

            // Persistir conta atualizada
            await _accountRepository.UpdateAsync(account);

            // Criar operação usando o OperationId da requisição (garante idempotência)
            var operation = new Operation(
                request.OperationId,
                request.AccountId,
                EOperationType.Debit,
                request.Amount
            );

            operation.Complete();
            await _operationRepository.AddAsync(operation);

            // Publicação do evento com retry exponencial
            const int maxAttempts = 3;
            int attempt = 0;
            int delayMs = 200;

            while (true)
            {
                try
                {
                    await _eventPublisher.PublishAsync(
                       new OperationCreatedEvent(
                           operation.Id,
                           operation.AccountId,
                           operation.Amount,
                           operation.Type.ToString()
                       )
                    );
                    break;
                }
                catch
                {
                    attempt++;
                    if (attempt >= maxAttempts)
                    {
                        // Marca operação como falhada e persiste
                        operation.Fail();
                        await _operationRepository.UpdateAsync(operation);
                        throw;
                    }

                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
            }
        }
    }
}