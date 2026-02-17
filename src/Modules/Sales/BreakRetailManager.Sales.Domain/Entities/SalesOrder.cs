namespace BreakRetailManager.Sales.Domain.Entities;

public sealed class SalesOrder
{
    private readonly List<SalesOrderLine> _lines = new();

    private SalesOrder()
    {
    }

    public SalesOrder(string number, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Order number is required.", nameof(number));
        }

        Id = Guid.NewGuid();
        Number = number;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Azure AD Object ID (oid claim) of the user who created the sale.</summary>
    public string? CreatedByObjectId { get; private set; }

    /// <summary>Display name snapshot of the user who created the sale.</summary>
    public string? CreatedByDisplayName { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public Guid LocationId { get; private set; }

    public string? Cae { get; private set; }

    public DateOnly? CaeExpirationDate { get; private set; }

    public long InvoiceNumber { get; private set; }

    public int PointOfSale { get; private set; }

    public int InvoiceType { get; private set; }

    public IReadOnlyCollection<SalesOrderLine> Lines => _lines;

    public decimal Subtotal => _lines.Sum(line => line.LineTotal);

    public decimal DiscountTotal { get; private set; }

    public decimal Total => Subtotal - DiscountTotal;

    public bool RequiresFiscalAuthorization => PaymentMethod == PaymentMethod.Card;

    public void SetCreatedBy(string objectId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            throw new ArgumentException("User object ID is required.", nameof(objectId));
        }

        CreatedByObjectId = objectId;
        CreatedByDisplayName = displayName;
    }

    public void SetPaymentMethod(PaymentMethod paymentMethod)
    {
        PaymentMethod = paymentMethod;
    }

    public void SetLocation(Guid locationId)
    {
        if (locationId == Guid.Empty)
        {
            throw new ArgumentException("Location is required.", nameof(locationId));
        }

        LocationId = locationId;
    }

    public void AddLine(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        _lines.Add(new SalesOrderLine(productId, productName, quantity, unitPrice));
    }

    public void SetDiscount(decimal discountTotal)
    {
        if (discountTotal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountTotal), "Discount cannot be negative.");
        }

        if (discountTotal > Subtotal)
        {
            throw new ArgumentOutOfRangeException(nameof(discountTotal), "Discount cannot exceed order subtotal.");
        }

        DiscountTotal = decimal.Round(discountTotal, 2, MidpointRounding.AwayFromZero);
    }

    public void SetFiscalAuthorization(string cae, DateOnly caeExpirationDate, long invoiceNumber, int pointOfSale, int invoiceType)
    {
        if (string.IsNullOrWhiteSpace(cae))
        {
            throw new ArgumentException("CAE is required.", nameof(cae));
        }

        Cae = cae;
        CaeExpirationDate = caeExpirationDate;
        InvoiceNumber = invoiceNumber;
        PointOfSale = pointOfSale;
        InvoiceType = invoiceType;
    }
}
