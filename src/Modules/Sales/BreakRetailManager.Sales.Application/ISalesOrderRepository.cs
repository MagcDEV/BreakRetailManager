using BreakRetailManager.Sales.Domain.Entities;

namespace BreakRetailManager.Sales.Application;

public interface ISalesOrderRepository
{
    Task<IReadOnlyList<SalesOrder>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SalesOrder order, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
