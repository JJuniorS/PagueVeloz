namespace PagueVeloz.Infrastructure.Persistence.Entities;

public class ClientEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<AccountEntity> Accounts { get; set; } = new List<AccountEntity>();
}
