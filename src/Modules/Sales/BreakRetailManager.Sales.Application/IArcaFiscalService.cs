namespace BreakRetailManager.Sales.Application;

public interface IArcaFiscalService
{
    Task<FiscalAuthorizationResult> AuthorizeAsync(
        decimal totalAmount,
        DateTimeOffset invoiceDate,
        CancellationToken cancellationToken = default);
}

public sealed record FiscalAuthorizationResult(
    string Cae,
    DateOnly CaeExpirationDate,
    long InvoiceNumber,
    int PointOfSale,
    int InvoiceType);
