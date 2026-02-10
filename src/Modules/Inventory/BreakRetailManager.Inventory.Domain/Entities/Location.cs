namespace BreakRetailManager.Inventory.Domain.Entities;

public sealed class Location
{
    private Location()
    {
    }

    public Location(string name, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Location name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name;
        Address = address;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void Update(string name, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Location name is required.", nameof(name));
        }

        Name = name;
        Address = address;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
