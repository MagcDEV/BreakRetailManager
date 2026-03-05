using BreakRetailManager.Sales.Contracts;
using BreakRetailManager.Sales.Domain.Entities;
using ContractOfferDiscountType = BreakRetailManager.Sales.Contracts.OfferDiscountType;
using DomainOfferDiscountType = BreakRetailManager.Sales.Domain.OfferDiscountType;

namespace BreakRetailManager.Sales.Application;

public static class OfferMappings
{
    public static OfferDto ToDto(Offer offer)
    {
        return new OfferDto(
            offer.Id,
            offer.Name,
            offer.Description,
            ToContractDiscountType(offer.DiscountType),
            offer.DiscountValue,
            offer.IsActive,
            offer.CreatedAt,
            offer.UpdatedAt,
            offer.Requirements
                .Select(requirement => new OfferRequirementDto(requirement.ProductId, requirement.Quantity))
                .ToList());
    }

    public static Offer FromCreateRequest(CreateOfferRequest request)
    {
        return new Offer(
            request.Name,
            request.Description,
            ToDomainDiscountType(request.DiscountType),
            request.DiscountValue,
            ToDomainRequirements(request.Requirements));
    }

    public static void ApplyUpdate(Offer offer, UpdateOfferRequest request)
    {
        offer.Update(
            request.Name,
            request.Description,
            ToDomainDiscountType(request.DiscountType),
            request.DiscountValue,
            ToDomainRequirements(request.Requirements));
    }

    private static IReadOnlyList<OfferRequirement> ToDomainRequirements(IReadOnlyList<OfferRequirementRequest> requirements)
    {
        if (requirements is null)
        {
            throw new ArgumentException("At least one offer requirement is required.", nameof(requirements));
        }

        return requirements
            .Select(requirement => new OfferRequirement(requirement.ProductId, requirement.Quantity))
            .ToList();
    }

    private static ContractOfferDiscountType ToContractDiscountType(DomainOfferDiscountType discountType)
    {
        return discountType switch
        {
            DomainOfferDiscountType.Percentage => ContractOfferDiscountType.Percentage,
            DomainOfferDiscountType.FixedAmount => ContractOfferDiscountType.FixedAmount,
            _ => throw new ArgumentOutOfRangeException(nameof(discountType), discountType, "Unsupported offer discount type.")
        };
    }

    private static DomainOfferDiscountType ToDomainDiscountType(ContractOfferDiscountType discountType)
    {
        return discountType switch
        {
            ContractOfferDiscountType.Percentage => DomainOfferDiscountType.Percentage,
            ContractOfferDiscountType.FixedAmount => DomainOfferDiscountType.FixedAmount,
            _ => throw new ArgumentOutOfRangeException(nameof(discountType), discountType, "Unsupported offer discount type.")
        };
    }
}
