using BreakRetailManager.AccountsControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.AccountsControl.Infrastructure.Data;

public sealed class AccountsControlDbContext : DbContext
{
    public AccountsControlDbContext(DbContextOptions<AccountsControlDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Movement> Movements => Set<Movement>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ConfigurationEntry> ConfigurationEntries => Set<ConfigurationEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(account =>
        {
            account.ToTable("Accounts", "accounts");
            account.HasKey(entity => entity.Id);
            account.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
            account.Property(entity => entity.NormalizedName).HasMaxLength(200).IsRequired();
            account.Property(entity => entity.Type).IsRequired();
            account.Property(entity => entity.IsActive).IsRequired();
            account.Property(entity => entity.CreatedAt).IsRequired();
            account.Property(entity => entity.UpdatedAt).IsRequired();
            account.Property(entity => entity.DeletedAt);
            account.HasIndex(entity => entity.NormalizedName).IsUnique();
            account.HasIndex(entity => new { entity.Type, entity.IsActive });
            account.HasIndex(entity => entity.CreatedAt).IsDescending();

            account.HasMany(entity => entity.Movements)
                .WithOne(entity => entity.Account)
                .HasForeignKey(entity => entity.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            account.Navigation(entity => entity.Movements).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Movement>(movement =>
        {
            movement.ToTable("Movements", "accounts");
            movement.HasKey(entity => entity.Id);
            movement.Property(entity => entity.Description).HasMaxLength(500).IsRequired();
            movement.Property(entity => entity.Amount).HasColumnType("decimal(18,2)").IsRequired();
            movement.Property(entity => entity.Shift).HasMaxLength(64);
            movement.Property(entity => entity.MovementType).IsRequired();
            movement.Property(entity => entity.IsAdminAdjustment).IsRequired();
            movement.Property(entity => entity.CreatedAt).IsRequired();
            movement.Property(entity => entity.CreatedByRole).IsRequired();
            movement.Property(entity => entity.CreatedByUserId).HasMaxLength(128);
            movement.HasIndex(entity => new { entity.AccountId, entity.CreatedAt });
            movement.HasIndex(entity => entity.CreatedAt).IsDescending();
        });

        modelBuilder.Entity<AuditLog>(audit =>
        {
            audit.ToTable("AuditLogs", "accounts");
            audit.HasKey(entity => entity.Id);
            audit.Property(entity => entity.Action).HasMaxLength(100).IsRequired();
            audit.Property(entity => entity.EntityType).HasMaxLength(100).IsRequired();
            audit.Property(entity => entity.EntityId).HasMaxLength(128).IsRequired();
            audit.Property(entity => entity.PerformedBy).HasMaxLength(128).IsRequired();
            audit.Property(entity => entity.PerformedAt).IsRequired();
            audit.Property(entity => entity.PayloadJson).HasColumnType("nvarchar(max)");
            audit.HasIndex(entity => entity.PerformedAt).IsDescending();
        });

        modelBuilder.Entity<ConfigurationEntry>(configuration =>
        {
            configuration.ToTable("Configurations", "accounts");
            configuration.HasKey(entity => entity.Id);
            configuration.Property(entity => entity.Key).HasMaxLength(100).IsRequired();
            configuration.Property(entity => entity.Value).HasColumnType("nvarchar(max)");
            configuration.Property(entity => entity.UpdatedAt).IsRequired();
            configuration.HasIndex(entity => entity.Key).IsUnique();
        });
    }
}
