using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.Services;
using PagueVeloz.Core.Enums;

namespace PagueVeloz.Application.UseCases;

public class RevertUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly OperationEventPublisher _eventPublisher;
    private readonly IAccountLockManager _lockManager;

    public RevertUseCase(
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

    public async Task ExecuteAsync(RevertRequest request)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId)
            ?? throw new InvalidOperationException("Account not found.");

        var originalOperation = await _operationRepository.GetByIdAsync(request.OperationId)
            ?? throw new InvalidOperationException("Operation not found.");

        using (await _lockManager.AcquireAsync(account.Id))
        {
            // Reverter a operação original
            originalOperation.Revert();

            // Reverter o saldo conforme o tipo de operação original
            switch (originalOperation.Type)
            {
                case EOperationType.Debit:
                    // Se foi débito, creditamos de volta
                    account.Credit(originalOperation.Amount);
                    break;
                case EOperationType.Credit:
                    // Se foi crédito, debitamos de volta
                    account.Debit(originalOperation.Amount);
                    break;
                case EOperationType.Reserve:
                    // Se foi reserva, liberamos
                    account.Release(originalOperation.Amount);
                    break;
                case EOperationType.Capture:
                    account.Credit(originalOperation.Amount);
                    break;
                default:
                    throw new InvalidOperationException($"Cannot revert operation type: {originalOperation.Type}");
            }

            // Persistir conta atualizada
            await _accountRepository.UpdateAsync(account);

            // Atualizar operação original como reverted
            await _operationRepository.UpdateAsync(originalOperation);

            // Publicação do evento com retry exponencial (centralizado)
            var @event = new OperationCreatedEvent(
                originalOperation.Id,
                originalOperation.AccountId,
                originalOperation.Amount,
                $"{originalOperation.Type}_REVERTED"
            );

            await _eventPublisher.PublishWithRetryAsync(@event);
        }
    }
}
