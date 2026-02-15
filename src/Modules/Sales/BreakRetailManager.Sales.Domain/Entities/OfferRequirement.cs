namespace BreakRetailManager.Sales.Domain.Entities;

public sealed class OfferRequirement
{
    private OfferRequirement()
    {
    }

    public OfferRequirement(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product is required.", nameof(productId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        ProductId = productId;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }
}
