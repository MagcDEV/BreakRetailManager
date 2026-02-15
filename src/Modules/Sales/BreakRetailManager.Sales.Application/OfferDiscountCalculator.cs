using BreakRetailManager.Sales.Domain;
using BreakRetailManager.Sales.Domain.Entities;

namespace BreakRetailManager.Sales.Application;

internal static class OfferDiscountCalculator
{
    public static decimal CalculateDiscount(SalesOrder order, IReadOnlyList<Offer> offers)
    {
        if (offers.Count == 0 || order.Lines.Count == 0)
        {
            return 0;
        }

        var buckets = order.Lines
            .GroupBy(line => line.ProductId)
            .ToDictionary(
                group => group.Key,
                group => new ProductBucket(
                    group.Sum(line => line.Quantity),
                    group.Sum(line => line.LineTotal)));

        var totalDiscount = 0m;

        foreach (var offer in offers
                     .Where(offer => offer.IsActive)
                     .OrderBy(offer => offer.CreatedAt)
                     .ThenBy(offer => offer.Id))
        {
            var applicationCount = GetApplicationCount(offer, buckets);
            if (applicationCount == 0)
            {
                continue;
            }

            var matchedAmount = ConsumeMatchedAmount(offer, buckets, applicationCount);
            var offerDiscount = CalculateOfferDiscount(offer, matchedAmount, applicationCount);
            totalDiscount += Math.Min(offerDiscount, matchedAmount);

            if (totalDiscount >= order.Subtotal)
            {
                return order.Subtotal;
            }
        }

        return decimal.Round(Math.Min(totalDiscount, order.Subtotal), 2, MidpointRounding.AwayFromZero);
    }

    private static int GetApplicationCount(Offer offer, IReadOnlyDictionary<Guid, ProductBucket> buckets)
    {
        var applicationCount = int.MaxValue;

        foreach (var requirement in offer.Requirements)
        {
            if (!buckets.TryGetValue(requirement.ProductId, out var bucket))
            {
                return 0;
            }

            var possibleApplications = bucket.Quantity / requirement.Quantity;
            if (possibleApplications == 0)
            {
                return 0;
            }

            applicationCount = Math.Min(applicationCount, possibleApplications);
        }

        return applicationCount == int.MaxValue ? 0 : applicationCount;
    }

    private static decimal ConsumeMatchedAmount(Offer offer, IReadOnlyDictionary<Guid, ProductBucket> buckets, int applicationCount)
    {
        var matchedAmount = 0m;

        foreach (var requirement in offer.Requirements)
        {
            var quantityToConsume = requirement.Quantity * applicationCount;
            var bucket = buckets[requirement.ProductId];
            matchedAmount += bucket.Consume(quantityToConsume);
        }

        return matchedAmount;
    }

    private static decimal CalculateOfferDiscount(Offer offer, decimal matchedAmount, int applicationCount)
    {
        var rawDiscount = offer.DiscountType switch
        {
            OfferDiscountType.Percentage => matchedAmount * (offer.DiscountValue / 100m),
            OfferDiscountType.FixedAmount => offer.DiscountValue * applicationCount,
            _ => throw new ArgumentOutOfRangeException(nameof(offer.DiscountType), offer.DiscountType, "Unsupported offer discount type.")
        };

        var limitedDiscount = Math.Min(rawDiscount, matchedAmount);
        return decimal.Round(limitedDiscount, 2, MidpointRounding.AwayFromZero);
    }

    private sealed class ProductBucket
    {
        public ProductBucket(int quantity, decimal amount)
        {
            Quantity = quantity;
            Amount = amount;
        }

        public int Quantity { get; private set; }

        public decimal Amount { get; private set; }

        public decimal Consume(int quantity)
        {
            if (quantity <= 0 || Quantity == 0)
            {
                return 0;
            }

            if (quantity > Quantity)
            {
                throw new InvalidOperationException("Cannot consume more quantity than available in product bucket.");
            }

            if (quantity == Quantity)
            {
                var allAmount = Amount;
                Quantity = 0;
                Amount = 0;
                return allAmount;
            }

            var consumed = Amount * quantity / Quantity;
            Quantity -= quantity;
            Amount -= consumed;
            return consumed;
        }
    }
}
