namespace BreakRetailManager.Sales.Contracts;

public sealed record CreateOfferRequest(
    string Name,
    string Description,
    OfferDiscountType DiscountType,
    decimal DiscountValue,
    IReadOnlyList<OfferRequirementRequest> Requirements);
