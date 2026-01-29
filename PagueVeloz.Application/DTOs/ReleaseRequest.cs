namespace PagueVeloz.Application.DTOs;

public record ReleaseRequest(
    Guid AccountId,
    Guid OperationId,
    decimal Amount
);
