using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BreakRetailManager.Sales.Infrastructure.Arca;

/// <summary>
/// Calls ARCA WSFE SOAP methods: FECompUltimoAutorizado, FECAESolicitar, FECompConsultar.
/// </summary>
public sealed class WsfeClient
{
    private static readonly XNamespace SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace ArNs = "http://ar.gov.afip.dif.FEV1/";

    private readonly HttpClient _httpClient;
    private readonly ArcaSettings _settings;
    private readonly ILogger<WsfeClient> _logger;

    public WsfeClient(HttpClient httpClient, IOptions<ArcaSettings> settings, ILogger<WsfeClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<long> GetLastAuthorizedInvoiceAsync(
        string token, string sign, CancellationToken cancellationToken = default)
    {
        var soap = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                           xmlns:ar="http://ar.gov.afip.dif.FEV1/">
              <soap:Body>
                <ar:FECompUltimoAutorizado>
                  <ar:Auth>
                    <ar:Token>{EscapeXml(token)}</ar:Token>
                    <ar:Sign>{EscapeXml(sign)}</ar:Sign>
                    <ar:Cuit>{_settings.Cuit}</ar:Cuit>
                  </ar:Auth>
                  <ar:PtoVta>{_settings.PointOfSale}</ar:PtoVta>
                  <ar:CbteTipo>{_settings.InvoiceType}</ar:CbteTipo>
                </ar:FECompUltimoAutorizado>
              </soap:Body>
            </soap:Envelope>
            """;

        var responseBody = await PostSoapAsync(soap, "FECompUltimoAutorizado", cancellationToken);
        var doc = XDocument.Parse(responseBody);

        var cbteNro = doc.Descendants(ArNs + "CbteNro").FirstOrDefault()?.Value
                      ?? throw new InvalidOperationException("CbteNro not found in FECompUltimoAutorizado response.");

        _logger.LogInformation("Last authorized invoice number: {CbteNro}", cbteNro);
        return long.Parse(cbteNro, CultureInfo.InvariantCulture);
    }

    public async Task<CaeResponse> RequestCaeAsync(
        string token, string sign, CaeRequest request, CancellationToken cancellationToken = default)
    {
        var invoiceDate = request.InvoiceDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var soap = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                           xmlns:ar="http://ar.gov.afip.dif.FEV1/">
              <soap:Body>
                <ar:FECAESolicitar>
                  <ar:Auth>
                    <ar:Token>{EscapeXml(token)}</ar:Token>
                    <ar:Sign>{EscapeXml(sign)}</ar:Sign>
                    <ar:Cuit>{_settings.Cuit}</ar:Cuit>
                  </ar:Auth>
                  <ar:FeCAEReq>
                    <ar:FeCabReq>
                      <ar:CantReg>1</ar:CantReg>
                      <ar:PtoVta>{_settings.PointOfSale}</ar:PtoVta>
                      <ar:CbteTipo>{_settings.InvoiceType}</ar:CbteTipo>
                    </ar:FeCabReq>
                    <ar:FeDetReq>
                      <ar:FECAEDetRequest>
                        <ar:Concepto>1</ar:Concepto>
                        <ar:DocTipo>{_settings.DocumentType}</ar:DocTipo>
                        <ar:DocNro>{_settings.DocumentNumber}</ar:DocNro>
                        <ar:CbteDesde>{request.InvoiceNumber}</ar:CbteDesde>
                        <ar:CbteHasta>{request.InvoiceNumber}</ar:CbteHasta>
                        <ar:CbteFch>{invoiceDate}</ar:CbteFch>
                        <ar:ImpTotal>{request.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)}</ar:ImpTotal>
                        <ar:ImpTotConc>0</ar:ImpTotConc>
                        <ar:ImpNeto>{request.NetAmount.ToString("F2", CultureInfo.InvariantCulture)}</ar:ImpNeto>
                        <ar:ImpOpEx>0</ar:ImpOpEx>
                        <ar:ImpTrib>0</ar:ImpTrib>
                        <ar:ImpIVA>{request.IvaAmount.ToString("F2", CultureInfo.InvariantCulture)}</ar:ImpIVA>
                        <ar:MonId>PES</ar:MonId>
                        <ar:MonCotiz>1</ar:MonCotiz>
                        <ar:CondicionIVAReceptorId>{_settings.CondicionIvaReceptorId}</ar:CondicionIVAReceptorId>
                        <ar:Iva>
                          <ar:AlicIva>
                            <ar:Id>5</ar:Id>
                            <ar:BaseImp>{request.NetAmount.ToString("F2", CultureInfo.InvariantCulture)}</ar:BaseImp>
                            <ar:Importe>{request.IvaAmount.ToString("F2", CultureInfo.InvariantCulture)}</ar:Importe>
                          </ar:AlicIva>
                        </ar:Iva>
                      </ar:FECAEDetRequest>
                    </ar:FeDetReq>
                  </ar:FeCAEReq>
                </ar:FECAESolicitar>
              </soap:Body>
            </soap:Envelope>
            """;

        var responseBody = await PostSoapAsync(soap, "FECAESolicitar", cancellationToken);
        return ParseCaeResponse(responseBody, request.InvoiceNumber);
    }

    public async Task<CaeResponse?> ConsultInvoiceAsync(
        string token, string sign, long invoiceNumber, CancellationToken cancellationToken = default)
    {
        var soap = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                           xmlns:ar="http://ar.gov.afip.dif.FEV1/">
              <soap:Body>
                <ar:FECompConsultar>
                  <ar:Auth>
                    <ar:Token>{EscapeXml(token)}</ar:Token>
                    <ar:Sign>{EscapeXml(sign)}</ar:Sign>
                    <ar:Cuit>{_settings.Cuit}</ar:Cuit>
                  </ar:Auth>
                  <ar:FeCompConsReq>
                    <ar:CbteTipo>{_settings.InvoiceType}</ar:CbteTipo>
                    <ar:CbteNro>{invoiceNumber}</ar:CbteNro>
                    <ar:PtoVta>{_settings.PointOfSale}</ar:PtoVta>
                  </ar:FeCompConsReq>
                </ar:FECompConsultar>
              </soap:Body>
            </soap:Envelope>
            """;

        try
        {
            var responseBody = await PostSoapAsync(soap, "FECompConsultar", cancellationToken);
            var doc = XDocument.Parse(responseBody);

            var resultNode = doc.Descendants(ArNs + "ResultGet").FirstOrDefault();
            if (resultNode is null) return null;

            var cae = resultNode.Element(ArNs + "CodAutorizacion")?.Value;
            var caeFchVto = resultNode.Element(ArNs + "FchVto")?.Value;

            if (string.IsNullOrWhiteSpace(cae) || string.IsNullOrWhiteSpace(caeFchVto))
                return null;

            return new CaeResponse(cae, caeFchVto, "A");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FECompConsultar failed for invoice {InvoiceNumber}", invoiceNumber);
            return null;
        }
    }

    private async Task<string> PostSoapAsync(string soapXml, string operation, CancellationToken cancellationToken)
    {
        _logger.LogDebug("WSFE {Operation} request to {Endpoint}:\n{Soap}", operation, _settings.WsfeEndpoint, soapXml);

        var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"http://ar.gov.afip.dif.FEV1/{operation}");

        var response = await _httpClient.PostAsync(_settings.WsfeEndpoint, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogDebug("WSFE {Operation} response ({StatusCode}):\n{Body}", operation, response.StatusCode, body);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("WSFE {Operation} returned {StatusCode}: {Body}", operation, response.StatusCode, body);
            throw new InvalidOperationException(
                $"WSFE {operation} failed with status {response.StatusCode}. Response: {body}");
        }

        return body;
    }

    private CaeResponse ParseCaeResponse(string responseBody, long invoiceNumber)
    {
        var doc = XDocument.Parse(responseBody);

        // Check header result
        var resultado = doc.Descendants(ArNs + "Resultado").FirstOrDefault()?.Value;

        // Check for errors
        var errors = doc.Descendants(ArNs + "Err")
            .Select(e => $"Code={e.Element(ArNs + "Code")?.Value}, Msg={e.Element(ArNs + "Msg")?.Value}")
            .ToList();

        if (errors.Count > 0)
        {
            _logger.LogWarning("WSFE FECAESolicitar errors: {Errors}", string.Join("; ", errors));
        }

        // Check observations
        var observations = doc.Descendants(ArNs + "Obs")
            .Select(o => $"Code={o.Element(ArNs + "Code")?.Value}, Msg={o.Element(ArNs + "Msg")?.Value}")
            .ToList();

        if (observations.Count > 0)
        {
            _logger.LogInformation("WSFE FECAESolicitar observations: {Obs}", string.Join("; ", observations));
        }

        var detResponse = doc.Descendants(ArNs + "FECAEDetResponse").FirstOrDefault()
                          ?? doc.Descendants(ArNs + "FEDetResponse").FirstOrDefault();

        if (detResponse is null)
        {
            // Resultado=R (rejected) with no detail — throw with errors
            var errorMsg = errors.Count > 0
                ? string.Join("; ", errors)
                : "Unknown error (no FECAEDetResponse in response)";
            throw new InvalidOperationException($"FECAESolicitar rejected for invoice {invoiceNumber}: {errorMsg}");
        }

        var detResultado = detResponse.Element(ArNs + "Resultado")?.Value ?? resultado;
        var cae = detResponse.Element(ArNs + "CAE")?.Value;
        var caeFchVto = detResponse.Element(ArNs + "CAEFchVto")?.Value;

        if (detResultado == "R" || string.IsNullOrWhiteSpace(cae))
        {
            var detErrors = detResponse.Descendants(ArNs + "Obs")
                .Select(o => $"Code={o.Element(ArNs + "Code")?.Value}, Msg={o.Element(ArNs + "Msg")?.Value}")
                .ToList();

            var errorDetail = detErrors.Count > 0
                ? string.Join("; ", detErrors)
                : (errors.Count > 0 ? string.Join("; ", errors) : "Unknown rejection reason");

            throw new InvalidOperationException($"FECAESolicitar rejected for invoice {invoiceNumber}: {errorDetail}");
        }

        return new CaeResponse(
            cae!,
            caeFchVto ?? throw new InvalidOperationException("CAEFchVto missing in approved response."),
            detResultado ?? "A");
    }

    private static string EscapeXml(string value)
    {
        return new XText(value).ToString();
    }
}

public sealed record CaeRequest(
    long InvoiceNumber,
    DateOnly InvoiceDate,
    decimal TotalAmount,
    decimal NetAmount,
    decimal IvaAmount);

public sealed record CaeResponse(string Cae, string CaeFchVto, string Resultado);
