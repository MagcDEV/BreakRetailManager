namespace BreakRetailManager.Sales.Contracts;

public sealed record OfferDto(
    Guid Id,
    string Name,
    string Description,
    OfferDiscountType DiscountType,
    decimal DiscountValue,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<OfferRequirementDto> Requirements);
