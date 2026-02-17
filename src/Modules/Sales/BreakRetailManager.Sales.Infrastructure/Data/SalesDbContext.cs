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

    public DbSet<Offer> Offers => Set<Offer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrder>(order =>
        {
            order.ToTable("SalesOrders", "sales");
            order.HasKey(entity => entity.Id);
            order.Property(entity => entity.Number).HasMaxLength(32).IsRequired();
            order.Property(entity => entity.CreatedAt).IsRequired();
            order.Property(entity => entity.CreatedByObjectId).HasMaxLength(128);
            order.Property(entity => entity.CreatedByDisplayName).HasMaxLength(256);
            order.Property(entity => entity.PaymentMethod).IsRequired();
            order.Property(entity => entity.LocationId).IsRequired();
            order.Property(entity => entity.DiscountTotal).HasColumnType("decimal(18,2)").IsRequired();
            order.Property(entity => entity.Cae).HasMaxLength(20);
            order.Property(entity => entity.CaeExpirationDate);
            order.Property(entity => entity.InvoiceNumber);
            order.Property(entity => entity.PointOfSale);
            order.Property(entity => entity.InvoiceType);

            order.OwnsMany(entity => entity.Lines, line =>
            {
                line.ToTable("SalesOrderLines", "sales");
                line.HasKey(entity => entity.Id);
                line.Property(entity => entity.ProductId).IsRequired();
                line.Property(entity => entity.ProductName).HasMaxLength(200).IsRequired();
                line.Property(entity => entity.Quantity).IsRequired();
                line.Property(entity => entity.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
                line.WithOwner().HasForeignKey("SalesOrderId");
            });

            order.Navigation(entity => entity.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Offer>(offer =>
        {
            offer.ToTable("Offers", "sales");
            offer.HasKey(entity => entity.Id);
            offer.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
            offer.Property(entity => entity.Description).HasMaxLength(1000).IsRequired();
            offer.Property(entity => entity.DiscountType).IsRequired();
            offer.Property(entity => entity.DiscountValue).HasColumnType("decimal(18,2)").IsRequired();
            offer.Property(entity => entity.IsActive).IsRequired();
            offer.Property(entity => entity.CreatedAt).IsRequired();
            offer.Property(entity => entity.UpdatedAt).IsRequired();

            offer.OwnsMany(entity => entity.Requirements, requirement =>
            {
                requirement.ToTable("OfferRequirements", "sales");
                requirement.WithOwner().HasForeignKey("OfferId");
                requirement.Property<Guid>("OfferId");
                requirement.Property(entity => entity.ProductId).ValueGeneratedNever().IsRequired();
                requirement.Property(entity => entity.Quantity).IsRequired();
                requirement.HasKey("OfferId", nameof(OfferRequirement.ProductId));
            });

            offer.Navigation(entity => entity.Requirements).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
