using BreakRetailManager.UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.UserManagement.Infrastructure.Data;

public sealed class UserManagementDbContext : DbContext
{
    public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<AppRole> Roles => Set<AppRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppRole>(role =>
        {
            role.ToTable("Roles", "users");
            role.HasKey(e => e.Id);
            role.Property(e => e.Name).HasMaxLength(64).IsRequired();
            role.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<AppUser>(user =>
        {
            user.ToTable("Users", "users");
            user.HasKey(e => e.Id);
            user.Property(e => e.ObjectId).HasMaxLength(128).IsRequired();
            user.HasIndex(e => e.ObjectId).IsUnique();
            user.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
            user.Property(e => e.Email).HasMaxLength(256).IsRequired();
            user.Property(e => e.CreatedAt).IsRequired();

            user.HasMany(e => e.Roles)
                .WithMany()
                .UsingEntity("UserRoles",
                    right => right.HasOne(typeof(AppRole)).WithMany().HasForeignKey("RoleId"),
                    left => left.HasOne(typeof(AppUser)).WithMany().HasForeignKey("UserId"),
                    join =>
                    {
                        join.ToTable("UserRoles", "users");
                        join.HasKey("UserId", "RoleId");
                    });

            user.Navigation(e => e.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
