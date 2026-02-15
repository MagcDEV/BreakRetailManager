using BreakRetailManager.Sales.Domain;

namespace BreakRetailManager.Sales.Domain.Entities;

public sealed class Offer
{
    private readonly List<OfferRequirement> _requirements = new();

    private Offer()
    {
    }

    public Offer(
        string name,
        string description,
        OfferDiscountType discountType,
        decimal discountValue,
        IReadOnlyList<OfferRequirement> requirements)
    {
        ApplyDefinition(name, description, discountType, discountValue, requirements);

        Id = Guid.NewGuid();
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public OfferDiscountType DiscountType { get; private set; }

    public decimal DiscountValue { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<OfferRequirement> Requirements => _requirements;

    public void Update(
        string name,
        string description,
        OfferDiscountType discountType,
        decimal discountValue,
        IReadOnlyList<OfferRequirement> requirements)
    {
        ApplyDefinition(name, description, discountType, discountValue, requirements);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void ApplyDefinition(
        string name,
        string description,
        OfferDiscountType discountType,
        decimal discountValue,
        IReadOnlyList<OfferRequirement> requirements)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Offer name is required.", nameof(name));
        }

        if (discountValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountValue), "Discount value must be greater than zero.");
        }

        if (discountType == OfferDiscountType.Percentage && discountValue > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(discountValue), "Percentage discount cannot exceed 100.");
        }

        if (requirements is null || requirements.Count == 0)
        {
            throw new ArgumentException("At least one offer requirement is required.", nameof(requirements));
        }

        var duplicateProductId = requirements
            .GroupBy(requirement => requirement.ProductId)
            .FirstOrDefault(group => group.Count() > 1)?
            .Key;

        if (duplicateProductId.HasValue)
        {
            throw new ArgumentException($"Product '{duplicateProductId}' appears more than once in the offer requirements.", nameof(requirements));
        }

        Name = name;
        Description = description?.Trim() ?? string.Empty;
        DiscountType = discountType;
        DiscountValue = decimal.Round(discountValue, 2, MidpointRounding.AwayFromZero);

        _requirements.Clear();
        _requirements.AddRange(requirements.Select(requirement => new OfferRequirement(requirement.ProductId, requirement.Quantity)));
    }
}
