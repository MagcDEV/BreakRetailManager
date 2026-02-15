namespace BreakRetailManager.Sales.Contracts;

public sealed record UpdateOfferRequest(
    string Name,
    string Description,
    OfferDiscountType DiscountType,
    decimal DiscountValue,
    IReadOnlyList<OfferRequirementRequest> Requirements);
