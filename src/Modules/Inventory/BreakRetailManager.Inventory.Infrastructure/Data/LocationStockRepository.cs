using BreakRetailManager.Inventory.Application;
using BreakRetailManager.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.Inventory.Infrastructure.Data;

public sealed class LocationStockRepository : ILocationStockRepository
{
    private readonly InventoryDbContext _dbContext;

    public LocationStockRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LocationStock?> GetAsync(Guid locationId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LocationStocks
            .Include(s => s.Location)
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.LocationId == locationId && s.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<LocationStock>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LocationStocks
            .AsNoTracking()
            .Include(s => s.Location)
            .Include(s => s.Product)
            .Where(s => s.LocationId == locationId)
            .OrderBy(s => s.Product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocationStock>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LocationStocks
            .AsNoTracking()
            .Include(s => s.Location)
            .Include(s => s.Product)
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.Location.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetTotalsByProductAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _dbContext.LocationStocks
            .AsNoTracking()
            .Where(stock => productIds.Contains(stock.ProductId))
            .GroupBy(stock => stock.ProductId)
            .Select(group => new { ProductId = group.Key, Total = group.Sum(stock => stock.Quantity) })
            .ToDictionaryAsync(item => item.ProductId, item => item.Total, cancellationToken);
    }

    public async Task<IReadOnlyList<LocationStock>> GetLowStockByLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LocationStocks
            .AsNoTracking()
            .Include(s => s.Location)
            .Include(s => s.Product)
            .Where(s => s.LocationId == locationId && s.Quantity <= s.ReorderLevel)
            .OrderBy(s => s.Quantity)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LocationStock stock, CancellationToken cancellationToken = default)
    {
        await _dbContext.LocationStocks.AddAsync(stock, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
