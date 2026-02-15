using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BreakRetailManager.Sales.Infrastructure.Arca;

/// <summary>
/// Loads the ARCA certificate from Azure Key Vault (production).
/// Uses DefaultAzureCredential which supports Managed Identity on App Service.
/// </summary>
public sealed class KeyVaultCertificateProvider : ICertificateProvider
{
    private readonly ArcaSettings _settings;
    private readonly ILogger<KeyVaultCertificateProvider> _logger;

    private X509Certificate2? _cached;
    private DateTimeOffset _cacheExpiry;

    public KeyVaultCertificateProvider(IOptions<ArcaSettings> settings, ILogger<KeyVaultCertificateProvider> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<X509Certificate2> GetCertificateAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null && _cacheExpiry > DateTimeOffset.UtcNow)
        {
            return _cached;
        }

        _logger.LogInformation("Loading certificate '{CertName}' from Key Vault {VaultUri}",
            _settings.CertificateName, _settings.KeyVaultUri);

        var client = new CertificateClient(new Uri(_settings.KeyVaultUri), new DefaultAzureCredential());

        var certWithPolicy = await client.GetCertificateAsync(_settings.CertificateName, cancellationToken);

        // Download the full certificate with private key (requires certificates/get + secrets/get permissions)
        var secret = await client.DownloadCertificateAsync(
            new DownloadCertificateOptions(_settings.CertificateName), cancellationToken);

        _cached = secret.Value;
        // Cache for 1 hour — Key Vault certs don't change often
        _cacheExpiry = DateTimeOffset.UtcNow.AddHours(1);

        _logger.LogInformation("Certificate loaded from Key Vault. Subject={Subject}, Expires={Expiry}",
            _cached.Subject, certWithPolicy.Value.Properties.ExpiresOn);

        return _cached;
    }
}
