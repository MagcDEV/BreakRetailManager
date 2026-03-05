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
        var query = from product in _dbContext.Products.AsNoTracking().Include(p => p.Provider)
                    join stockGroup in _dbContext.Set<LocationStock>()
                        .GroupBy(s => s.ProductId)
                        .Select(g => new { ProductId = g.Key, Total = g.Sum(s => s.Quantity) })
                    on product.Id equals stockGroup.ProductId into stockJoin
                    from stock in stockJoin.DefaultIfEmpty()
                    let total = stock != null ? stock.Total : 0
                    where total <= product.ReorderLevel
                    orderby total
                    select new { Product = product, StockTotal = total };

        var results = await query.ToListAsync(cancellationToken);
        return results.Select(r => (r.Product, r.StockTotal)).ToList();
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
