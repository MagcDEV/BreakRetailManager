namespace BreakRetailManager.Inventory.Contracts;

public sealed record ProviderDto(
    Guid Id,
    string Name,
    string ContactName,
    string Phone,
    string Email,
    string Address,
    DateTimeOffset CreatedAt);
