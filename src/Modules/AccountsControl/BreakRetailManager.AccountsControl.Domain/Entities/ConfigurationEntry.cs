namespace BreakRetailManager.AccountsControl.Domain.Entities;

public sealed class ConfigurationEntry
{
    private ConfigurationEntry()
    {
    }

    public ConfigurationEntry(string key, string value, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        Id = Guid.NewGuid();
        Key = key.Trim();
        Value = value ?? string.Empty;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string value, DateTimeOffset updatedAt)
    {
        Value = value ?? string.Empty;
        UpdatedAt = updatedAt;
    }
}
