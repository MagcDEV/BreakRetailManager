using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BreakRetailManager.Sales.Infrastructure.Arca;

/// <summary>
/// Loads the ARCA certificate from local PEM files (development).
/// </summary>
public sealed class FileCertificateProvider : ICertificateProvider
{
    private readonly ArcaSettings _settings;
    private readonly ILogger<FileCertificateProvider> _logger;

    public FileCertificateProvider(IOptions<ArcaSettings> settings, ILogger<FileCertificateProvider> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<X509Certificate2> GetCertificateAsync(CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(_settings.CertificatePath);
        if (extension.Equals(".pfx", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".p12", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Loading certificate from PFX file: {CertPath}", _settings.CertificatePath);
            var keyStorageFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet;
            var password = string.IsNullOrWhiteSpace(_settings.CertificatePassword)
                ? null
                : _settings.CertificatePassword;

            try
            {
                var cert = X509CertificateLoader.LoadPkcs12FromFile(
                    _settings.CertificatePath,
                    password,
                    keyStorageFlags);
                return Task.FromResult(cert);
            }
            catch (CryptographicException) when (!string.IsNullOrWhiteSpace(_settings.CertificatePassword))
            {
                _logger.LogWarning(
                    "Failed to load PFX with configured password, retrying without password. CertPath={CertPath}",
                    _settings.CertificatePath);

                var cert = X509CertificateLoader.LoadPkcs12FromFile(
                    _settings.CertificatePath,
                    null,
                    keyStorageFlags);
                return Task.FromResult(cert);
            }
        }

        _logger.LogDebug("Loading certificate from PEM files: {CertPath}", _settings.CertificatePath);

        var pemCert = X509Certificate2.CreateFromPemFile(_settings.CertificatePath, _settings.PrivateKeyPath);
        // On Windows, re-export to PKCS12 so the private key is usable for CMS signing
        var exported = X509CertificateLoader.LoadPkcs12(pemCert.Export(X509ContentType.Pfx), null);
        pemCert.Dispose();

        return Task.FromResult(exported);
    }
}
