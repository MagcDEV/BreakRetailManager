using BreakRetailManager.Sales.Application;
using BreakRetailManager.Sales.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.Sales.Infrastructure.Data;

public sealed class OfferRepository : IOfferRepository
{
    private readonly SalesDbContext _dbContext;

    public OfferRepository(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Offer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Offers
            .AsNoTracking()
            .Include(offer => offer.Requirements)
            .OrderByDescending(offer => offer.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Offer>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Offers
            .AsNoTracking()
            .Include(offer => offer.Requirements)
            .Where(offer => offer.IsActive)
            .OrderBy(offer => offer.CreatedAt)
            .ThenBy(offer => offer.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Offers
            .Include(offer => offer.Requirements)
            .FirstOrDefaultAsync(offer => offer.Id == id, cancellationToken);
    }

    public async Task AddAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        await _dbContext.Offers.AddAsync(offer, cancellationToken);
    }

    public void Remove(Offer offer)
    {
        _dbContext.Offers.Remove(offer);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
