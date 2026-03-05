using BreakRetailManager.Sales.Application;
using BreakRetailManager.Sales.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.Sales.Infrastructure.Data;

public sealed class SalesOrderRepository : ISalesOrderRepository
{
    private readonly SalesDbContext _dbContext;

    public SalesOrderRepository(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SalesOrder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SalesOrders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Lines)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<SalesOrder> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SalesOrders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Lines)
            .OrderByDescending(order => order.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(SalesOrder order, CancellationToken cancellationToken = default)
    {
        await _dbContext.SalesOrders.AddAsync(order, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
