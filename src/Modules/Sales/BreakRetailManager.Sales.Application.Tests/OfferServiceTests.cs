using BreakRetailManager.Sales.Contracts;
using BreakRetailManager.Sales.Domain.Entities;
using ContractOfferDiscountType = BreakRetailManager.Sales.Contracts.OfferDiscountType;

namespace BreakRetailManager.Sales.Application.Tests;

public sealed class OfferServiceTests
{
    [Fact]
    public async Task CreateOfferAsync_CreatesActiveOffer()
    {
        var productId = Guid.NewGuid();
        var service = CreateService();

        var created = await service.CreateOfferAsync(new CreateOfferRequest(
            "Coca Promo",
            "2x Coca",
            ContractOfferDiscountType.Percentage,
            10m,
            [new OfferRequirementRequest(productId, 2)]));

        Assert.Equal("Coca Promo", created.Name);
        Assert.True(created.IsActive);
        Assert.Single(created.Requirements);
        Assert.Equal(productId, created.Requirements[0].ProductId);
    }

    [Fact]
    public async Task UpdateOfferAsync_UpdatesOfferDefinition()
    {
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var service = CreateService();

        var created = await service.CreateOfferAsync(new CreateOfferRequest(
            "Combo A",
            string.Empty,
            ContractOfferDiscountType.FixedAmount,
            5m,
            [new OfferRequirementRequest(firstProductId, 1)]));

        var updated = await service.UpdateOfferAsync(
            created.Id,
            new UpdateOfferRequest(
                "Combo A+B",
                "Updated",
                ContractOfferDiscountType.FixedAmount,
                15m,
                [
                    new OfferRequirementRequest(firstProductId, 2),
                    new OfferRequirementRequest(secondProductId, 1)
                ]));

        Assert.NotNull(updated);
        Assert.Equal("Combo A+B", updated!.Name);
        Assert.Equal(15m, updated.DiscountValue);
        Assert.Equal(2, updated.Requirements.Count);
    }

    [Fact]
    public async Task ActivateAndDeactivateOfferAsync_UpdatesActiveOffersList()
    {
        var productId = Guid.NewGuid();
        var service = CreateService();

        var created = await service.CreateOfferAsync(new CreateOfferRequest(
            "Temporary Offer",
            string.Empty,
            ContractOfferDiscountType.Percentage,
            20m,
            [new OfferRequirementRequest(productId, 1)]));

        var deactivated = await service.DeactivateOfferAsync(created.Id);
        Assert.NotNull(deactivated);
        Assert.False(deactivated!.IsActive);
        Assert.Empty(await service.GetActiveOffersAsync());

        var activated = await service.ActivateOfferAsync(created.Id);
        Assert.NotNull(activated);
        Assert.True(activated!.IsActive);
        Assert.Single(await service.GetActiveOffersAsync());
    }

    [Fact]
    public async Task DeleteOfferAsync_RemovesOffer()
    {
        var productId = Guid.NewGuid();
        var service = CreateService();

        var created = await service.CreateOfferAsync(new CreateOfferRequest(
            "Delete me",
            string.Empty,
            ContractOfferDiscountType.FixedAmount,
            10m,
            [new OfferRequirementRequest(productId, 1)]));

        var deleted = await service.DeleteOfferAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(created.Id));
    }

    private static OfferService CreateService()
    {
        return new OfferService(new InMemoryOfferRepository());
    }

    private sealed class InMemoryOfferRepository : IOfferRepository
    {
        private readonly List<Offer> _offers = [];

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
}
