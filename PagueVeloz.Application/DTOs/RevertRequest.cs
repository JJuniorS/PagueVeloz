namespace PagueVeloz.Application.DTOs;

public record RevertRequest(
    Guid AccountId,
    Guid OperationId
);
