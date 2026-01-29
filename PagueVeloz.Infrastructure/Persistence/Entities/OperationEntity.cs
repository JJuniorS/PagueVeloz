namespace PagueVeloz.Infrastructure.Persistence.Entities;

public class OperationEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Type { get; set; } = string.Empty; // Debit, Credit, Transfer, etc.
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Reverted
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public AccountEntity? Account { get; set; }
}
