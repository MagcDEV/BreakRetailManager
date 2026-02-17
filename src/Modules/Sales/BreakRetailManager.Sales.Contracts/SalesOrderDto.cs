namespace BreakRetailManager.Sales.Contracts;

public sealed record SalesOrderDto(
    Guid Id,
    string Number,
    DateTimeOffset CreatedAt,
    decimal Total,
    IReadOnlyList<SalesOrderLineDto> Lines,
    PaymentMethod PaymentMethod,
    Guid LocationId,
    string? Cae,
    DateOnly? CaeExpirationDate,
    long InvoiceNumber,
    int PointOfSale,
    decimal Subtotal = 0,
    decimal DiscountTotal = 0,
    string? CreatedByObjectId = null,
    string? CreatedByDisplayName = null);
