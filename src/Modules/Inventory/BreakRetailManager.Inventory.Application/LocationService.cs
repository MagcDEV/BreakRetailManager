using BreakRetailManager.Inventory.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace BreakRetailManager.Inventory.Application;

public sealed class LocationService
{
    private const string LocationsCacheKey = "locations-active";

    private readonly ILocationRepository _repository;
    private readonly IMemoryCache _cache;

    public LocationService(ILocationRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<IReadOnlyList<LocationDto>> GetLocationsAsync(CancellationToken cancellationToken)
    {
        var cached = await _cache.GetOrCreateAsync(LocationsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var locations = await _repository.GetActiveAsync(cancellationToken);
            return locations.Select(InventoryMappings.ToDto).ToList() as IReadOnlyList<LocationDto>;
        });

        return cached ?? [];
    }

    public async Task<LocationDto?> GetLocationByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var location = await _repository.GetByIdAsync(id, cancellationToken);
        return location is null ? null : InventoryMappings.ToDto(location);
    }

    public async Task<LocationDto> CreateLocationAsync(CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var location = new Domain.Entities.Location(request.Name, request.Address);
        await _repository.AddAsync(location, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        _cache.Remove(LocationsCacheKey);
        return InventoryMappings.ToDto(location);
    }
}
