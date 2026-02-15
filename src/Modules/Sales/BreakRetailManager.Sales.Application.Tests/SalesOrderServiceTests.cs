using BreakRetailManager.Sales.Contracts;
using BreakRetailManager.Sales.Domain.Entities;
using DomainOfferDiscountType = BreakRetailManager.Sales.Domain.OfferDiscountType;

namespace BreakRetailManager.Sales.Application.Tests;

public sealed class SalesOrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_AppliesPercentageOfferDiscount()
    {
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        var offer = new Offer(
            "2x Coca Cola",
            "10% off buying two Coca Cola",
            DomainOfferDiscountType.Percentage,
            10m,
            [new OfferRequirement(productId, 2)]);

        var service = CreateService([offer], out var orderRepository, out var fiscalService);
        var request = CreateRequest(
            locationId,
            [new CreateSalesOrderLineRequest(productId, "Coca Cola", 2, 100m)]);

        var created = await service.CreateOrderAsync(request);

        Assert.Equal(200m, created.Subtotal);
        Assert.Equal(20m, created.DiscountTotal);
        Assert.Equal(180m, created.Total);
        Assert.Single(await orderRepository.GetAllAsync());
        Assert.Equal(0, fiscalService.Calls);
    }

    [Fact]
    public async Task CreateOrderAsync_AppliesFixedAmountBundleDiscount()
    {
        var productAId = Guid.NewGuid();
        var productBId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        var offer = new Offer(
            "Combo A+B",
            "Fixed discount for combo",
            DomainOfferDiscountType.FixedAmount,
            20m,
            [
                new OfferRequirement(productAId, 2),
                new OfferRequirement(productBId, 1)
            ]);

        var service = CreateService([offer], out _, out _);
        var request = CreateRequest(
            locationId,
            [
                new CreateSalesOrderLineRequest(productAId, "Coca Cola", 2, 50m),
                new CreateSalesOrderLineRequest(productBId, "Ice bag", 1, 30m)
            ]);

        var created = await service.CreateOrderAsync(request);

        Assert.Equal(130m, created.Subtotal);
        Assert.Equal(20m, created.DiscountTotal);
        Assert.Equal(110m, created.Total);
    }

    [Fact]
    public async Task CreateOrderAsync_CapsDiscountAtSubtotal()
    {
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        var offer = new Offer(
            "Huge discount",
            string.Empty,
            DomainOfferDiscountType.FixedAmount,
            500m,
            [new OfferRequirement(productId, 1)]);

        var service = CreateService([offer], out _, out _);
        var request = CreateRequest(
            locationId,
            [new CreateSalesOrderLineRequest(productId, "Coca Cola", 1, 100m)]);

        var created = await service.CreateOrderAsync(request);

        Assert.Equal(100m, created.Subtotal);
        Assert.Equal(100m, created.DiscountTotal);
        Assert.Equal(0m, created.Total);
    }

    [Fact]
    public async Task CreateOrderAsync_DoesNotApplyInactiveOffer()
    {
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        var offer = new Offer(
            "Inactive offer",
            string.Empty,
            DomainOfferDiscountType.Percentage,
            25m,
            [new OfferRequirement(productId, 2)]);
        offer.Deactivate();

        var service = CreateService([offer], out _, out _);
        var request = CreateRequest(
            locationId,
            [new CreateSalesOrderLineRequest(productId, "Coca Cola", 2, 100m)]);

        var created = await service.CreateOrderAsync(request);

        Assert.Equal(200m, created.Subtotal);
        Assert.Equal(0m, created.DiscountTotal);
        Assert.Equal(200m, created.Total);
    }

    [Fact]
    public async Task CreateOrderAsync_AppliesOfferMultipleTimes_WhenQuantityAllows()
    {
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        var offer = new Offer(
            "2x Soda combo",
            string.Empty,
            DomainOfferDiscountType.FixedAmount,
            10m,
            [new OfferRequirement(productId, 2)]);

        var service = CreateService([offer], out _, out _);
        var request = CreateRequest(
            locationId,
            [new CreateSalesOrderLineRequest(productId, "Soda", 5, 20m)]);

        var created = await service.CreateOrderAsync(request);

        Assert.Equal(100m, created.Subtotal);
        Assert.Equal(20m, created.DiscountTotal);
        Assert.Equal(80m, created.Total);
    }

    private static SalesOrderService CreateService(
        IReadOnlyList<Offer> offers,
        out InMemorySalesOrderRepository orderRepository,
        out FakeArcaFiscalService fiscalService)
    {
        orderRepository = new InMemorySalesOrderRepository();
        fiscalService = new FakeArcaFiscalService();
        var offerRepository = new InMemoryOfferRepository(offers);
        return new SalesOrderService(orderRepository, offerRepository, fiscalService);
    }

    private static CreateSalesOrderRequest CreateRequest(
        Guid locationId,
        IReadOnlyList<CreateSalesOrderLineRequest> lines,
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        return new CreateSalesOrderRequest(lines, locationId, paymentMethod);
    }

    private sealed class InMemorySalesOrderRepository : ISalesOrderRepository
    {
        private readonly List<SalesOrder> _orders = [];

        public Task<IReadOnlyList<SalesOrder>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SalesOrder>>(_orders.ToList());
        }

        public Task AddAsync(SalesOrder order, CancellationToken cancellationToken = default)
        {
            _orders.Add(order);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOfferRepository : IOfferRepository
    {
        private readonly List<Offer> _offers;

        public InMemoryOfferRepository(IEnumerable<Offer> offers)
        {
            _offers = offers.ToList();
        }

        public Task<IReadOnlyList<Offer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Offer>>(_offers.ToList());
        }

        public Task<IReadOnlyList<Offer>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Offer>>(_offers.Where(offer => offer.IsActive).ToList());
        }

        public Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_offers.FirstOrDefault(offer => offer.Id == id));
        }

        public Task AddAsync(Offer offer, CancellationToken cancellationToken = default)
        {
            _offers.Add(offer);
            return Task.CompletedTask;
        }

        public void Remove(Offer offer)
        {
            _offers.Remove(offer);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeArcaFiscalService : IArcaFiscalService
    {
        public int Calls { get; private set; }

        public Task<FiscalAuthorizationResult> AuthorizeAsync(
            decimal totalAmount,
            DateTimeOffset invoiceDate,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new FiscalAuthorizationResult(
                "CAE-TEST",
                DateOnly.FromDateTime(invoiceDate.UtcDateTime),
                InvoiceNumber: 1,
                PointOfSale: 1,
                InvoiceType: 1));
        }
    }
}
