using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.Services;
using PagueVeloz.Core.Entities;
using PagueVeloz.Core.Enums;

namespace PagueVeloz.Application.UseCases;

public class TransferUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly OperationEventPublisher _eventPublisher;
    private readonly IAccountLockManager _lockManager;

    public TransferUseCase(
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

    public async Task ExecuteAsync(TransferRequest request)
    {
        // Validar que não é transferência para a mesma conta
        if (request.SourceAccountId == request.DestinationAccountId)
            throw new InvalidOperationException("Cannot transfer to the same account.");

        var sourceAccount = await _accountRepository.GetByIdAsync(request.SourceAccountId)
            ?? throw new InvalidOperationException("Source account not found.");

        var destinationAccount = await _accountRepository.GetByIdAsync(request.DestinationAccountId)
            ?? throw new InvalidOperationException("Destination account not found.");

        // Adquirir locks em ordem consistente (por ID) para evitar deadlock
        var firstAccountId = request.SourceAccountId < request.DestinationAccountId
            ? request.SourceAccountId
            : request.DestinationAccountId;

        var secondAccountId = request.SourceAccountId < request.DestinationAccountId
            ? request.DestinationAccountId
            : request.SourceAccountId;

        using (await _lockManager.AcquireAsync(firstAccountId))
        using (await _lockManager.AcquireAsync(secondAccountId))
        {
            // Idempotência
            var existingOperation = await _operationRepository.GetByIdAsync(request.OperationId);
            if (existingOperation is not null)
                return;

            // Executar regras de negócio
            sourceAccount.Debit(request.Amount);
            destinationAccount.Credit(request.Amount);

            // Persistir contas atualizado
            await _accountRepository.UpdateAsync(sourceAccount);
            await _accountRepository.UpdateAsync(destinationAccount);

            // Criar operação
            var operation = new Operation(
                request.OperationId,
                request.SourceAccountId,
                EOperationType.Transfer,
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
