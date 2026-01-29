namespace PagueVeloz.Application.DTOs;

public record CreditRequest(
    Guid AccountId,
    Guid OperationId,
    decimal Amount
);
