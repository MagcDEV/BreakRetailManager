using BreakRetailManager.Sales.Domain.Entities;

namespace BreakRetailManager.Sales.Application;

public interface IOfferRepository
{
    Task<IReadOnlyList<Offer>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Offer>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Offer offer, CancellationToken cancellationToken = default);

    void Remove(Offer offer);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
