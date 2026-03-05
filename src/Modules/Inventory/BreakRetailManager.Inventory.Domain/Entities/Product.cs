namespace BreakRetailManager.Inventory.Domain.Entities;

public sealed class Product
{
    private Product()
    {
    }

    public Product(
        string barcode,
        string name,
        string description,
        string category,
        decimal costPrice,
        decimal salePrice,
        int reorderLevel,
        Guid providerId)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new ArgumentException("Barcode is required.", nameof(barcode));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (costPrice < 0)
        {
            throw new ArgumentException("Cost price cannot be negative.", nameof(costPrice));
        }

        if (salePrice < 0)
        {
            throw new ArgumentException("Sale price cannot be negative.", nameof(salePrice));
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentException("Reorder level cannot be negative.", nameof(reorderLevel));
        }

        Id = Guid.NewGuid();
        Barcode = barcode;
        Name = name;
        Description = description;
        Category = category;
        CostPrice = costPrice;
        SalePrice = salePrice;
        ReorderLevel = reorderLevel;
        ProviderId = providerId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Barcode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public decimal CostPrice { get; private set; }

    public decimal SalePrice { get; private set; }

    public int ReorderLevel { get; private set; }

    public Guid ProviderId { get; private set; }

    public Provider Provider { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void UpdateDetails(
        string barcode,
        string name,
        string description,
        string category,
        decimal costPrice,
        decimal salePrice,
        int reorderLevel,
        Guid providerId)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new ArgumentException("Barcode is required.", nameof(barcode));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (costPrice < 0)
        {
            throw new ArgumentException("Cost price cannot be negative.", nameof(costPrice));
        }

        if (salePrice < 0)
        {
            throw new ArgumentException("Sale price cannot be negative.", nameof(salePrice));
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentException("Reorder level cannot be negative.", nameof(reorderLevel));
        }

        Barcode = barcode;
        Name = name;
        Description = description;
        Category = category;
        CostPrice = costPrice;
        SalePrice = salePrice;
        ReorderLevel = reorderLevel;
        ProviderId = providerId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
