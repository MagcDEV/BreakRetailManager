using BreakRetailManager.Sales.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace BreakRetailManager.Sales.Application;

public sealed class OfferService
{
    private const string ActiveOffersCacheKey = "active-offers";
    internal const string ActiveOffersDomainCacheKey = "active-offers-domain";

    private readonly IOfferRepository _repository;
    private readonly IMemoryCache _cache;

    public OfferService(IOfferRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<IReadOnlyList<OfferDto>> GetOffersAsync(CancellationToken cancellationToken = default)
    {
        var offers = await _repository.GetAllAsync(cancellationToken);
        return offers.Select(OfferMappings.ToDto).ToList();
    }

    public async Task<IReadOnlyList<OfferDto>> GetActiveOffersAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetOrCreateAsync(ActiveOffersCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            var offers = await _repository.GetActiveAsync(cancellationToken);
            return offers.Select(OfferMappings.ToDto).ToList() as IReadOnlyList<OfferDto>;
        });

        return cached ?? [];
    }

    public async Task<OfferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offer = await _repository.GetByIdAsync(id, cancellationToken);
        return offer is null ? null : OfferMappings.ToDto(offer);
    }

    public async Task<OfferDto> CreateOfferAsync(CreateOfferRequest request, CancellationToken cancellationToken = default)
    {
        var offer = OfferMappings.FromCreateRequest(request);
        await _repository.AddAsync(offer, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        InvalidateOfferCaches();
        return OfferMappings.ToDto(offer);
    }

    public async Task<OfferDto?> UpdateOfferAsync(Guid id, UpdateOfferRequest request, CancellationToken cancellationToken = default)
    {
        var offer = await _repository.GetByIdAsync(id, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        OfferMappings.ApplyUpdate(offer, request);
        await _repository.SaveChangesAsync(cancellationToken);
        InvalidateOfferCaches();
        return OfferMappings.ToDto(offer);
    }

    public async Task<OfferDto?> ActivateOfferAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offer = await _repository.GetByIdAsync(id, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        offer.Activate();
        await _repository.SaveChangesAsync(cancellationToken);
        InvalidateOfferCaches();
        return OfferMappings.ToDto(offer);
    }

    public async Task<OfferDto?> DeactivateOfferAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offer = await _repository.GetByIdAsync(id, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        offer.Deactivate();
        await _repository.SaveChangesAsync(cancellationToken);
        InvalidateOfferCaches();
        return OfferMappings.ToDto(offer);
    }

    public async Task<bool> DeleteOfferAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var offer = await _repository.GetByIdAsync(id, cancellationToken);
        if (offer is null)
        {
            return false;
        }

        _repository.Remove(offer);
        await _repository.SaveChangesAsync(cancellationToken);
        InvalidateOfferCaches();
        return true;
    }

    private void InvalidateOfferCaches()
    {
        _cache.Remove(ActiveOffersCacheKey);
        _cache.Remove(ActiveOffersDomainCacheKey);
    }
}
