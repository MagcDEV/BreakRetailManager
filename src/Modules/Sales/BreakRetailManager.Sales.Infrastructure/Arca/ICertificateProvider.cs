using System.Security.Cryptography.X509Certificates;

namespace BreakRetailManager.Sales.Infrastructure.Arca;

/// <summary>
/// Abstracts certificate loading for ARCA WSAA signing.
/// </summary>
public interface ICertificateProvider
{
    Task<X509Certificate2> GetCertificateAsync(CancellationToken cancellationToken = default);
}
