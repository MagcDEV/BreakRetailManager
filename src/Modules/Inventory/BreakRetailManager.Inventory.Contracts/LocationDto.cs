namespace BreakRetailManager.Inventory.Contracts;

public sealed record LocationDto(
    Guid Id,
    string Name,
    string Address,
    bool IsActive,
    DateTimeOffset CreatedAt);
