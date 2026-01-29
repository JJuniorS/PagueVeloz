using PagueVeloz.Core.Enums;
using System.Buffers;

namespace PagueVeloz.Core.Entities;

public class Operation
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public EOperationType Type { get; private set; }
    public EOperationStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    protected Operation() { }

    public Operation(Guid accountId, EOperationType type, decimal amount)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount;
        Status = EOperationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = EOperationStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        Status = EOperationStatus.Failed;
    }

    public void Revert()
    {
        Status = EOperationStatus.Reverted;
    }
}
