namespace PagueVeloz.Infrastructure.Persistence.Entities;

public class AccountEntity
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public decimal Balance { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal ReservedBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ClientEntity? Client { get; set; }
    public ICollection<OperationEntity> Operations { get; set; } = new List<OperationEntity>();
}
