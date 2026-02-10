namespace BreakRetailManager.Sales.Infrastructure.Arca;

public sealed class ArcaSettings
{
    public const string SectionName = "Arca";

    public string CertificatePath { get; set; } = string.Empty;

    public string PrivateKeyPath { get; set; } = string.Empty;

    public long Cuit { get; set; }

    public int PointOfSale { get; set; }

    public int InvoiceType { get; set; } = 6; // Factura B

    public int DocumentType { get; set; } = 99; // Consumidor Final

    public long DocumentNumber { get; set; } // 0 for Consumidor Final

    public int CondicionIvaReceptorId { get; set; } = 5; // 5 = Consumidor Final

    public string Environment { get; set; } = "Homologation";

    public string WsaaEndpoint => Environment == "Production"
        ? "https://wsaa.afip.gov.ar/ws/services/LoginCms"
        : "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

    public string WsfeEndpoint => Environment == "Production"
        ? "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
        : "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
}
