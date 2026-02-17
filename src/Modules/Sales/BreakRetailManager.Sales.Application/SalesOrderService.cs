using BreakRetailManager.Sales.Contracts;

namespace BreakRetailManager.Sales.Application;

public sealed class SalesOrderService
{
    private readonly ISalesOrderRepository _repository;
    private readonly IOfferRepository _offerRepository;
    private readonly IArcaFiscalService _fiscalService;

    public SalesOrderService(ISalesOrderRepository repository, IOfferRepository offerRepository, IArcaFiscalService fiscalService)
    {
        _repository = repository;
        _offerRepository = offerRepository;
        _fiscalService = fiscalService;
    }

    public async Task<IReadOnlyList<SalesOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _repository.GetAllAsync(cancellationToken);
        return orders.Select(SalesMappings.ToDto).ToList();
    }

    public async Task<SalesOrderDto> CreateOrderAsync(
        CreateSalesOrderRequest request,
        string createdByObjectId,
        string createdByDisplayName,
        CancellationToken cancellationToken = default)
    {
        var order = SalesMappings.FromRequest(request);
        order.SetCreatedBy(createdByObjectId, createdByDisplayName);
        var activeOffers = await _offerRepository.GetActiveAsync(cancellationToken);
        var totalDiscount = OfferDiscountCalculator.CalculateDiscount(order, activeOffers);
        order.SetDiscount(totalDiscount);

        if (order.RequiresFiscalAuthorization)
        {
            var fiscal = await _fiscalService.AuthorizeAsync(order.Total, order.CreatedAt, cancellationToken);
            order.SetFiscalAuthorization(
                fiscal.Cae,
                fiscal.CaeExpirationDate,
                fiscal.InvoiceNumber,
                fiscal.PointOfSale,
                fiscal.InvoiceType);
        }

        await _repository.AddAsync(order, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return SalesMappings.ToDto(order);
    }
}
