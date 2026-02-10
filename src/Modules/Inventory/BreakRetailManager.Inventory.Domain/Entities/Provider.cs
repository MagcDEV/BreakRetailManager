namespace BreakRetailManager.Inventory.Domain.Entities;

public sealed class Provider
{
    private Provider()
    {
    }

    public Provider(string name, string contactName, string phone, string email, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name;
        ContactName = contactName;
        Phone = phone;
        Email = email;
        Address = address;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string ContactName { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public void Update(string name, string contactName, string phone, string email, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name is required.", nameof(name));
        }

        Name = name;
        ContactName = contactName;
        Phone = phone;
        Email = email;
        Address = address;
    }
}
