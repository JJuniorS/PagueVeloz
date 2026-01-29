namespace PagueVeloz.Application.DTOs;

public record CaptureRequest(
    Guid AccountId,
    Guid OperationId,
    decimal Amount
);
