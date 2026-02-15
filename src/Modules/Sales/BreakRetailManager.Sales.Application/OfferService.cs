using BreakRetailManager.Sales.Contracts;

namespace BreakRetailManager.Sales.Application;

public sealed class OfferService
{
    private readonly IOfferRepository _repository;

    public OfferService(IOfferRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<OfferDto>> GetOffersAsync(CancellationToken cancellationToken = default)
    {
        var offers = await _repository.GetAllAsync(cancellationToken);
        return offers.Select(OfferMappings.ToDto).ToList();
    }

    public async Task<IReadOnlyList<OfferDto>> GetActiveOffersAsync(CancellationToken cancellationToken = default)
    {
        var offers = await _repository.GetActiveAsync(cancellationToken);
        return offers.Select(OfferMappings.ToDto).ToList();
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
        return true;
    }
}
