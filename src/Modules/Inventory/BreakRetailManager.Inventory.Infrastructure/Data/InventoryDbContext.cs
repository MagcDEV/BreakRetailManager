using BreakRetailManager.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.Inventory.Infrastructure.Data;

public sealed class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<LocationStock> LocationStocks => Set<LocationStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Provider>(provider =>
        {
            provider.ToTable("Providers", "inventory");
            provider.HasKey(entity => entity.Id);
            provider.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
            provider.Property(entity => entity.ContactName).HasMaxLength(200);
            provider.Property(entity => entity.Phone).HasMaxLength(50);
            provider.Property(entity => entity.Email).HasMaxLength(200);
            provider.Property(entity => entity.Address).HasMaxLength(500);
            provider.Property(entity => entity.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("Products", "inventory");
            product.HasKey(entity => entity.Id);
            product.Property(entity => entity.Barcode).HasMaxLength(100).IsRequired();
            product.HasIndex(entity => entity.Barcode).IsUnique();
            product.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
            product.Property(entity => entity.Description).HasMaxLength(1000);
            product.Property(entity => entity.Category).HasMaxLength(100);
            product.Property(entity => entity.CostPrice).HasColumnType("decimal(18,2)").IsRequired();
            product.Property(entity => entity.SalePrice).HasColumnType("decimal(18,2)").IsRequired();
            product.Property(entity => entity.StockQuantity).IsRequired();
            product.Property(entity => entity.ReorderLevel).IsRequired();
            product.Property(entity => entity.CreatedAt).IsRequired();
            product.Property(entity => entity.UpdatedAt).IsRequired();
            product.Ignore(entity => entity.IsLowStock);

            product.HasOne(entity => entity.Provider)
                .WithMany()
                .HasForeignKey(entity => entity.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Location>(location =>
        {
            location.ToTable("Locations", "inventory");
            location.HasKey(e => e.Id);
            location.Property(e => e.Name).HasMaxLength(200).IsRequired();
            location.Property(e => e.Address).HasMaxLength(500);
            location.Property(e => e.IsActive).IsRequired();
            location.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<LocationStock>(stock =>
        {
            stock.ToTable("LocationStocks", "inventory");
            stock.HasKey(e => e.Id);
            stock.HasIndex(e => new { e.LocationId, e.ProductId }).IsUnique();
            stock.Property(e => e.Quantity).IsRequired();
            stock.Property(e => e.ReorderLevel).IsRequired();
            stock.Ignore(e => e.IsLowStock);

            stock.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            stock.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
