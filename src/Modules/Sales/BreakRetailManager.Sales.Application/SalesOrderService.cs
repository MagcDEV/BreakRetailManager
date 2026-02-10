using BreakRetailManager.Sales.Contracts;

namespace BreakRetailManager.Sales.Application;

public sealed class SalesOrderService
{
    private readonly ISalesOrderRepository _repository;
    private readonly IArcaFiscalService _fiscalService;

    public SalesOrderService(ISalesOrderRepository repository, IArcaFiscalService fiscalService)
    {
        _repository = repository;
        _fiscalService = fiscalService;
    }

    public async Task<IReadOnlyList<SalesOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _repository.GetAllAsync(cancellationToken);
        return orders.Select(SalesMappings.ToDto).ToList();
    }

    public async Task<SalesOrderDto> CreateOrderAsync(
        CreateSalesOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = SalesMappings.FromRequest(request);

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
