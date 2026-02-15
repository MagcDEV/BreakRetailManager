using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BreakRetailManager.Sales.Infrastructure.Arca;

/// <summary>
/// Authenticates with ARCA WSAA to obtain Token and Sign for WSFE calls.
/// Caches the ticket until near expiration (~12 h validity).
/// </summary>
public sealed class WsaaClient
{
    private static readonly XNamespace SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace WsaaNs = "http://wsaa.view.sua.dvadac.desein.afip.gov";

    private readonly HttpClient _httpClient;
    private readonly ArcaSettings _settings;
    private readonly ICertificateProvider _certificateProvider;
    private readonly ILogger<WsaaClient> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private WsaaTicket? _cachedTicket;

    public WsaaClient(HttpClient httpClient, IOptions<ArcaSettings> settings, ICertificateProvider certificateProvider, ILogger<WsaaClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _certificateProvider = certificateProvider;
        _logger = logger;
    }

    public async Task<WsaaTicket> GetTicketAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedTicket is not null && _cachedTicket.ExpirationTime > DateTime.UtcNow.AddMinutes(5))
        {
            return _cachedTicket;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedTicket is not null && _cachedTicket.ExpirationTime > DateTime.UtcNow.AddMinutes(5))
            {
                return _cachedTicket;
            }

            _logger.LogInformation("Requesting new WSAA ticket for service wsfe at {Endpoint}", _settings.WsaaEndpoint);

            var loginTicketXml = BuildLoginTicketRequest();
            _logger.LogDebug("WSAA LoginTicketRequest XML:\n{Xml}", loginTicketXml);

            byte[] signedCms;
            try
            {
                signedCms = await SignWithCertificateAsync(loginTicketXml, cancellationToken);
                _logger.LogDebug("CMS signed successfully, {Bytes} bytes", signedCms.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sign LoginTicketRequest");
                throw;
            }

            var base64Cms = Convert.ToBase64String(signedCms);
            var soapEnvelope = BuildSoapEnvelope(base64Cms);

            _logger.LogDebug("WSAA SOAP request (CMS omitted for brevity) to {Endpoint}", _settings.WsaaEndpoint);

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "\"\"");

            var response = await _httpClient.PostAsync(_settings.WsaaEndpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("WSAA response ({StatusCode}):\n{Body}", response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                // coe.alreadyAuthenticated: a valid ticket from a previous run still exists on the server.
                // Wait for it to expire and retry once.
                if (responseBody.Contains("coe.alreadyAuthenticated", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "WSAA: certificate already has a valid ticket. Waiting 30s for expiration before retrying...");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                    content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                    content.Headers.Add("SOAPAction", "\"\"");
                    response = await _httpClient.PostAsync(_settings.WsaaEndpoint, content, cancellationToken);
                    responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogDebug("WSAA retry response ({StatusCode}):\n{Body}", response.StatusCode, responseBody);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("WSAA returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                    throw new InvalidOperationException(
                        $"WSAA authentication failed with status {response.StatusCode}. Response: {responseBody}");
                }
            }

            _cachedTicket = ParseTicketResponse(responseBody);
            _logger.LogInformation("WSAA ticket obtained, expires at {Expiration}", _cachedTicket.ExpirationTime);
            return _cachedTicket;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static string BuildLoginTicketRequest()
    {
        var now = DateTime.UtcNow;
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <loginTicketRequest version="1.0">
              <header>
                <uniqueId>{now.Ticks % uint.MaxValue}</uniqueId>
                <generationTime>{now.AddMinutes(-10):yyyy-MM-ddTHH:mm:ssZ}</generationTime>
                <expirationTime>{now.AddMinutes(2):yyyy-MM-ddTHH:mm:ssZ}</expirationTime>
              </header>
              <service>wsfe</service>
            </loginTicketRequest>
            """;
    }

    private async Task<byte[]> SignWithCertificateAsync(string content, CancellationToken cancellationToken)
    {
        using var cert = await _certificateProvider.GetCertificateAsync(cancellationToken);

        var contentInfo = new ContentInfo(Encoding.UTF8.GetBytes(content));
        var cms = new SignedCms(contentInfo);
        var signer = new CmsSigner(cert)
        {
            IncludeOption = X509IncludeOption.EndCertOnly
        };

        cms.ComputeSignature(signer);
        return cms.Encode();
    }

    private static string BuildSoapEnvelope(string base64Cms)
    {
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:wsaa="http://wsaa.view.sua.dvadac.desein.afip.gov">
              <soapenv:Header/>
              <soapenv:Body>
                <wsaa:loginCms>
                  <wsaa:in0>{base64Cms}</wsaa:in0>
                </wsaa:loginCms>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    private static WsaaTicket ParseTicketResponse(string soapResponse)
    {
        var doc = XDocument.Parse(soapResponse);
        var returnElement = doc.Descendants(WsaaNs + "loginCmsReturn").FirstOrDefault()
                            ?? doc.Descendants("loginCmsReturn").FirstOrDefault();

        if (returnElement is null)
        {
            throw new InvalidOperationException("WSAA response does not contain loginCmsReturn element.");
        }

        // The return value is an XML string embedded in the SOAP response
        var ticketXml = XDocument.Parse(returnElement.Value);
        var credentials = ticketXml.Descendants("credentials").First();
        var header = ticketXml.Descendants("header").First();

        var token = credentials.Element("token")?.Value
                    ?? throw new InvalidOperationException("Token not found in WSAA response.");
        var sign = credentials.Element("sign")?.Value
                   ?? throw new InvalidOperationException("Sign not found in WSAA response.");
        var expirationTime = DateTime.Parse(
            header.Element("expirationTime")?.Value
            ?? throw new InvalidOperationException("ExpirationTime not found in WSAA response."));

        return new WsaaTicket(token, sign, expirationTime);
    }
}

public sealed record WsaaTicket(string Token, string Sign, DateTime ExpirationTime);
