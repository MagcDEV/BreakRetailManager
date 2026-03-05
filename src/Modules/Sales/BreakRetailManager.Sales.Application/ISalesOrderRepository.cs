using BreakRetailManager.Sales.Domain.Entities;

namespace BreakRetailManager.Sales.Application;

public interface ISalesOrderRepository
{
    Task<IReadOnlyList<SalesOrder>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SalesOrder> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task AddAsync(SalesOrder order, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
