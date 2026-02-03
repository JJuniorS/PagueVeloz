using Microsoft.EntityFrameworkCore;
using PagueVeloz.Infrastructure.Persistence.Entities;

namespace PagueVeloz.Infrastructure.Persistence;

public class PagueVelozDbContext : DbContext
{
    public PagueVelozDbContext(DbContextOptions<PagueVelozDbContext> options) : base(options) { }

    public DbSet<ClientEntity> Clients { get; set; } = null!;
    public DbSet<AccountEntity> Accounts { get; set; } = null!;
    public DbSet<OperationEntity> Operations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar Client
        modelBuilder.Entity<ClientEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasMany(e => e.Accounts)
                .WithOne(a => a.Client)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configurar Account
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.AvailableBalance).HasPrecision(18, 2);
            entity.Property(e => e.ReservedBalance).HasPrecision(18, 2);
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasMany(e => e.Operations)
                .WithOne(o => o.Account)
                .HasForeignKey(o => o.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configurar Operation
        modelBuilder.Entity<OperationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.AccountId, e.CreatedAt });
        });
    }
}

