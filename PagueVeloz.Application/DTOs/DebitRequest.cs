namespace PagueVeloz.Application.DTOs;

public record DebitRequest(
    Guid AccountId,
    Guid OperationId,
    decimal Amount
);
