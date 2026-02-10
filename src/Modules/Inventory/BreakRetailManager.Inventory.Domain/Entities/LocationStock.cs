namespace BreakRetailManager.Inventory.Domain.Entities;

public sealed class LocationStock
{
    private LocationStock()
    {
    }

    public LocationStock(Guid locationId, Guid productId, int quantity, int reorderLevel)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(quantity));
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentException("Reorder level cannot be negative.", nameof(reorderLevel));
        }

        Id = Guid.NewGuid();
        LocationId = locationId;
        ProductId = productId;
        Quantity = quantity;
        ReorderLevel = reorderLevel;
    }

    public Guid Id { get; private set; }

    public Guid LocationId { get; private set; }

    public Location Location { get; private set; } = null!;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public int Quantity { get; private set; }

    public int ReorderLevel { get; private set; }

    public bool IsLowStock => Quantity <= ReorderLevel;

    public void UpdateQuantity(int delta)
    {
        if (Quantity + delta < 0)
        {
            throw new InvalidOperationException("Insufficient stock at this location.");
        }

        Quantity += delta;
    }

    public void SetReorderLevel(int level)
    {
        if (level < 0)
        {
            throw new ArgumentException("Reorder level cannot be negative.", nameof(level));
        }

        ReorderLevel = level;
    }
}
