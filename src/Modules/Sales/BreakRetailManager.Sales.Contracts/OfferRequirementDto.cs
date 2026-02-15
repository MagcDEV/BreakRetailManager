namespace BreakRetailManager.Sales.Contracts;

public sealed record OfferRequirementDto(
    Guid ProductId,
    int Quantity);
