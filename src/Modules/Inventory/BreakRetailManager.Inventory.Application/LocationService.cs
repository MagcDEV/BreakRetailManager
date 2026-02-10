using BreakRetailManager.Inventory.Contracts;

namespace BreakRetailManager.Inventory.Application;

public sealed class LocationService
{
    private readonly ILocationRepository _repository;

    public LocationService(ILocationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<LocationDto>> GetLocationsAsync(CancellationToken cancellationToken)
    {
        var locations = await _repository.GetActiveAsync(cancellationToken);
        return locations.Select(InventoryMappings.ToDto).ToList();
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
        return InventoryMappings.ToDto(location);
    }
}
