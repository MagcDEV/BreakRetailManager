using BreakRetailManager.Inventory.Domain.Entities;

namespace BreakRetailManager.Inventory.Application;

public interface ILocationStockRepository
{
    Task<LocationStock?> GetAsync(Guid locationId, Guid productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationStock>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationStock>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetTotalsByProductAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationStock>> GetLowStockByLocationAsync(Guid locationId, CancellationToken cancellationToken = default);

    Task AddAsync(LocationStock stock, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
