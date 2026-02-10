using BreakRetailManager.Inventory.Domain.Entities;

namespace BreakRetailManager.Inventory.Application;

public interface IProviderRepository
{
    Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Provider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Provider provider, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
