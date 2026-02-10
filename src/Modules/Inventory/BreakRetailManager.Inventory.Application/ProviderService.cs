using BreakRetailManager.Inventory.Contracts;

namespace BreakRetailManager.Inventory.Application;

public sealed class ProviderService
{
    private readonly IProviderRepository _providerRepository;

    public ProviderService(IProviderRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<IReadOnlyList<ProviderDto>> GetProvidersAsync(CancellationToken cancellationToken)
    {
        var providers = await _providerRepository.GetAllAsync(cancellationToken);
        return providers.Select(InventoryMappings.ToDto).ToList();
    }

    public async Task<ProviderDto?> GetProviderByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.GetByIdAsync(id, cancellationToken);
        return provider is null ? null : InventoryMappings.ToDto(provider);
    }

    public async Task<ProviderDto> CreateProviderAsync(CreateProviderRequest request, CancellationToken cancellationToken)
    {
        var provider = InventoryMappings.ToProvider(request);
        await _providerRepository.AddAsync(provider, cancellationToken);
        await _providerRepository.SaveChangesAsync(cancellationToken);
        return InventoryMappings.ToDto(provider);
    }

    public async Task<ProviderDto?> UpdateProviderAsync(Guid id, UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.GetByIdAsync(id, cancellationToken);
        if (provider is null)
        {
            return null;
        }

        provider.Update(
            request.Name,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address);

        await _providerRepository.SaveChangesAsync(cancellationToken);
        return InventoryMappings.ToDto(provider);
    }
}
