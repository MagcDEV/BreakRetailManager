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
            .Include(order => order.Lines)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
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
