using System.Globalization;
using BreakRetailManager.Sales.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BreakRetailManager.Sales.Infrastructure.Arca;

/// <summary>
/// Orchestrates WSAA authentication and WSFE CAE requests.
/// Flow: authenticate → get last invoice number → request CAE → handle timeouts via FECompConsultar.
/// </summary>
public sealed class ArcaFiscalService : IArcaFiscalService
{
    private readonly WsaaClient _wsaaClient;
    private readonly WsfeClient _wsfeClient;
    private readonly ArcaSettings _settings;
    private readonly ILogger<ArcaFiscalService> _logger;

    public ArcaFiscalService(
        WsaaClient wsaaClient,
        WsfeClient wsfeClient,
        IOptions<ArcaSettings> settings,
        ILogger<ArcaFiscalService> logger)
    {
        _wsaaClient = wsaaClient;
        _wsfeClient = wsfeClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<FiscalAuthorizationResult> AuthorizeAsync(
        decimal totalAmount,
        DateTimeOffset invoiceDate,
        CancellationToken cancellationToken = default)
    {
        // 1. Authenticate with WSAA
        var ticket = await _wsaaClient.GetTicketAsync(cancellationToken);

        // 2. Get last authorized invoice number to maintain correlativity
        var lastNumber = await _wsfeClient.GetLastAuthorizedInvoiceAsync(
            ticket.Token, ticket.Sign, cancellationToken);
        var nextNumber = lastNumber + 1;

        _logger.LogInformation(
            "Requesting CAE for invoice {InvoiceNumber} (PtoVta={PtoVta}, CbteTipo={CbteTipo})",
            nextNumber, _settings.PointOfSale, _settings.InvoiceType);

        // 3. Split total into net + 21% IVA (prices are IVA-inclusive)
        var netAmount = Math.Round(totalAmount / 1.21m, 2);
        var ivaAmount = totalAmount - netAmount;

        var request = new CaeRequest(
            InvoiceNumber: nextNumber,
            InvoiceDate: DateOnly.FromDateTime(invoiceDate.UtcDateTime),
            TotalAmount: totalAmount,
            NetAmount: netAmount,
            IvaAmount: ivaAmount);

        CaeResponse caeResponse;
        try
        {
            caeResponse = await _wsfeClient.RequestCaeAsync(
                ticket.Token, ticket.Sign, request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Timeout / network failure — try to recover via FECompConsultar
            _logger.LogWarning(ex, "FECAESolicitar failed, attempting FECompConsultar for invoice {InvoiceNumber}", nextNumber);

            var consulted = await _wsfeClient.ConsultInvoiceAsync(
                ticket.Token, ticket.Sign, nextNumber, cancellationToken);

            if (consulted is null)
            {
                throw new InvalidOperationException(
                    $"CAE request failed and invoice {nextNumber} was not found via FECompConsultar. " +
                    "Do not resubmit without verifying correlativity.", ex);
            }

            caeResponse = consulted;
        }

        _logger.LogInformation("CAE obtained: {CAE}, expires: {CaeFchVto}", caeResponse.Cae, caeResponse.CaeFchVto);

        return new FiscalAuthorizationResult(
            caeResponse.Cae,
            DateOnly.ParseExact(caeResponse.CaeFchVto, "yyyyMMdd", CultureInfo.InvariantCulture),
            nextNumber,
            _settings.PointOfSale,
            _settings.InvoiceType);
    }
}
