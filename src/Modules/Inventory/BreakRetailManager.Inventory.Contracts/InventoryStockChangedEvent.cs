namespace BreakRetailManager.Inventory.Contracts;

public sealed record InventoryStockChangedEvent(
    Guid ProductId,
    Guid LocationId,
    int LocationQuantity,
    DateTimeOffset ChangedAtUtc);