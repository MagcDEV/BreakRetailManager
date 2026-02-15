namespace BreakRetailManager.Sales.Domain.Entities;

public sealed class SalesOrderLine
{
    private SalesOrderLine()
    {
    }

    public SalesOrderLine(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product is required.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be greater than zero.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
