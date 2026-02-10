namespace BreakRetailManager.UserManagement.Domain.Entities;

public sealed class AppRole
{
    private AppRole()
    {
    }

    public AppRole(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
