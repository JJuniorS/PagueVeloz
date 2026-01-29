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

    public DebitUseCase(
        IAccountRepository accountRepository,
        IOperationRepository operationRepository,
        IEventPublisher eventPublisher)
    {
        _accountRepository = accountRepository;
        _operationRepository = operationRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task ExecuteAsync(DebitRequest request)
    {
        // Idempotência
        var existingOperation = await _operationRepository.GetByIdAsync(request.OperationId);
        if (existingOperation is not null)
            return;

        // Buscar conta
        var account = await _accountRepository.GetByIdAsync(request.AccountId)
            ?? throw new InvalidOperationException("Account not found.");

        // Executar regra de negócio
        account.Debit(request.Amount);

        // Persistir conta
        await _accountRepository.UpdateAsync(account);

        // Criar operação APÓS sucesso
        var operation = new Operation(
            request.OperationId,
            request.AccountId,
            EOperationType.Debit,
            request.Amount
        );

        operation.Complete();
        await _operationRepository.AddAsync(operation);

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
        }
        catch
        {
            operation.Fail();
            await _operationRepository.UpdateAsync(operation);
            throw;
        }
    }
}
