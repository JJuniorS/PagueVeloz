namespace PagueVeloz.Application.DTOs;

public record ReserveRequest(
    Guid AccountId,
    Guid OperationId,
    decimal Amount
);
