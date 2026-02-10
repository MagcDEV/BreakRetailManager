using BreakRetailManager.Sales.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.Sales.Infrastructure.Data;

public sealed class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options)
        : base(options)
    {
    }

    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrder>(order =>
        {
            order.ToTable("SalesOrders", "sales");
            order.HasKey(entity => entity.Id);
            order.Property(entity => entity.Number).HasMaxLength(32).IsRequired();
            order.Property(entity => entity.CreatedAt).IsRequired();
            order.Property(entity => entity.PaymentMethod).IsRequired();
            order.Property(entity => entity.LocationId).IsRequired();
            order.Property(entity => entity.Cae).HasMaxLength(20);
            order.Property(entity => entity.CaeExpirationDate);
            order.Property(entity => entity.InvoiceNumber);
            order.Property(entity => entity.PointOfSale);
            order.Property(entity => entity.InvoiceType);

            order.OwnsMany(entity => entity.Lines, line =>
            {
                line.ToTable("SalesOrderLines", "sales");
                line.HasKey(entity => entity.Id);
                line.Property(entity => entity.ProductName).HasMaxLength(200).IsRequired();
                line.Property(entity => entity.Quantity).IsRequired();
                line.Property(entity => entity.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
                line.WithOwner().HasForeignKey("SalesOrderId");
            });

            order.Navigation(entity => entity.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
