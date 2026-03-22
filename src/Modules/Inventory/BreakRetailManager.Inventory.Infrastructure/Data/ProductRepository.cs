using BreakRetailManager.Inventory.Application;
using BreakRetailManager.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.Inventory.Infrastructure.Data;

public sealed class ProductRepository : IProductRepository
{
    private readonly InventoryDbContext _dbContext;

    public ProductRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Provider)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Provider)
            .OrderBy(product => product.Name);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Include(product => product.Provider)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Provider)
            .FirstOrDefaultAsync(product => product.Barcode == barcode, cancellationToken);
    }

    public async Task<IReadOnlyList<(Product Product, int StockTotal)>> GetLowStockWithTotalsAsync(CancellationToken cancellationToken = default)
    {
        var stockTotals = await _dbContext.Set<LocationStock>()
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(s => s.ProductId, s => s.Total, cancellationToken);

        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Provider)
            .ToListAsync(cancellationToken);

        return products
            .Select(p => (Product: p, StockTotal: stockTotals.GetValueOrDefault(p.Id, 0)))
            .Where(x => x.StockTotal <= x.Product.ReorderLevel)
            .OrderBy(x => x.StockTotal)
            .ToList();
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
