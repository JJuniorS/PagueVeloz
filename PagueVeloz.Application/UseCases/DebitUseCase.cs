using System.Diagnostics;
using PagueVeloz.Application.DTOs;
using PagueVeloz.Application.Events;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Application.Services;
using PagueVeloz.Core.Entities;
using PagueVeloz.Core.Enums;

namespace PagueVeloz.Application.UseCases;

public class DebitUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly OperationEventPublisher _eventPublisher;
    private readonly IAccountLockManager _lockManager;
    private readonly IOperationLogger _logger;

    public DebitUseCase(
        IAccountRepository accountRepository,
        IOperationRepository operationRepository,
        OperationEventPublisher eventPublisher,
        IAccountLockManager lockManager,
        IOperationLogger logger)
    {
        _accountRepository = accountRepository;
        _operationRepository = operationRepository;
        _eventPublisher = eventPublisher;
        _lockManager = lockManager;
        _logger = logger;
    }

    public async Task ExecuteAsync(DebitRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogOperationStarted(request.AccountId, "Debit", request.Amount);

        try
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
                {
                    _logger.LogIdempotencyDetected(request.OperationId);
                    return;
                }

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

                // Publicação do evento com retry exponencial (centralizado)
                var @event = new OperationCreatedEvent(
                    operation.Id,
                    operation.AccountId,
                    operation.Amount,
                    operation.Type.ToString()
                );

                await _eventPublisher.PublishWithRetryAsync(@event);
            }

            stopwatch.Stop();
            _logger.LogOperationCompleted(request.AccountId, "Debit", request.Amount, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogOperationFailed(request.AccountId, "Debit", ex.Message, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}