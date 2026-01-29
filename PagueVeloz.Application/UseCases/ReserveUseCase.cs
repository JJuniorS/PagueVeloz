using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.Services;
using PagueVeloz.Core.Entities;
using PagueVeloz.Core.Enums;

namespace PagueVeloz.Application.UseCases;

public class ReserveUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly OperationEventPublisher _eventPublisher;
    private readonly IAccountLockManager _lockManager;

    public ReserveUseCase(
        IAccountRepository accountRepository,
        IOperationRepository operationRepository,
        OperationEventPublisher eventPublisher,
        IAccountLockManager lockManager)
    {
        _accountRepository = accountRepository;
        _operationRepository = operationRepository;
        _eventPublisher = eventPublisher;
        _lockManager = lockManager;
    }

    public async Task ExecuteAsync(ReserveRequest request)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId)
            ?? throw new InvalidOperationException("Account not found.");

        using (await _lockManager.AcquireAsync(account.Id))
        {
            // Idempotência
            var existingOperation = await _operationRepository.GetByIdAsync(request.OperationId);
            if (existingOperation is not null)
                return;

            // Executar regra de negócio
            account.Reserve(request.Amount);

            // Persistir conta atualizada
            await _accountRepository.UpdateAsync(account);

            // Criar operação
            var operation = new Operation(
                request.OperationId,
                request.AccountId,
                EOperationType.Reserve,
                request.Amount
            );

            operation.Complete();
            await _operationRepository.AddAsync(operation);

            // Publicação do evento com retry exponencial (centralizado)
            var @event = new OperationCreatedEvent(
                operation.Id,
                operation.AccountId,
                operation.Amount,
                operation.Type.ToString()
            );

            await _eventPublisher.PublishWithRetryAsync(@event);
        }
    }
}
