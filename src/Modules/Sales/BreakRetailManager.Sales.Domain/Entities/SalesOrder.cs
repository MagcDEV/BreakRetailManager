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

    public PaymentMethod PaymentMethod { get; private set; }

    public Guid LocationId { get; private set; }

    public string? Cae { get; private set; }

    public DateOnly? CaeExpirationDate { get; private set; }

    public long InvoiceNumber { get; private set; }

    public int PointOfSale { get; private set; }

    public int InvoiceType { get; private set; }

    public IReadOnlyCollection<SalesOrderLine> Lines => _lines;

    public decimal Total => _lines.Sum(line => line.LineTotal);

    public bool RequiresFiscalAuthorization => PaymentMethod == PaymentMethod.Card;

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

    public void AddLine(string productName, int quantity, decimal unitPrice)
    {
        _lines.Add(new SalesOrderLine(productName, quantity, unitPrice));
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
