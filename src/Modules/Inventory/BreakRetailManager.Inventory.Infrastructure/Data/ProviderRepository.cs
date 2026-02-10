using BreakRetailManager.Inventory.Application;
using BreakRetailManager.Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.Inventory.Infrastructure.Data;

public sealed class ProviderRepository : IProviderRepository
{
    private readonly InventoryDbContext _dbContext;

    public ProviderRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Providers
            .AsNoTracking()
            .OrderBy(provider => provider.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Provider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Providers
            .FirstOrDefaultAsync(provider => provider.Id == id, cancellationToken);
    }

    public async Task AddAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        await _dbContext.Providers.AddAsync(provider, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
