namespace BreakRetailManager.Inventory.Contracts;

public sealed record UpdateProviderRequest(
    string Name,
    string ContactName,
    string Phone,
    string Email,
    string Address);
