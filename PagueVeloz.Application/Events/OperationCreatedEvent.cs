namespace PagueVeloz.Application.Events;

public record OperationCreatedEvent(
    Guid OperationId,
    Guid AccountId,
    decimal Amount,
    string Type
);
