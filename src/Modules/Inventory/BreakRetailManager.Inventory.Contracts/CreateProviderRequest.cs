namespace BreakRetailManager.Inventory.Contracts;

public sealed record CreateProviderRequest(
    string Name,
    string ContactName,
    string Phone,
    string Email,
    string Address);
