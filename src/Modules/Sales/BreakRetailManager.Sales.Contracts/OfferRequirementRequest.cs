namespace BreakRetailManager.Sales.Contracts;

public sealed record OfferRequirementRequest(
    Guid ProductId,
    int Quantity);
