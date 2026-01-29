namespace PagueVeloz.Application.DTOs;

public record TransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    Guid OperationId,
    decimal Amount
);
