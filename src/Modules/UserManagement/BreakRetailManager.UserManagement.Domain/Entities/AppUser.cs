namespace BreakRetailManager.UserManagement.Domain.Entities;

public sealed class AppUser
{
    private readonly List<AppRole> _roles = new();

    private AppUser()
    {
    }

    public AppUser(string objectId, string displayName, string email)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            throw new ArgumentException("Azure AD Object ID is required.", nameof(objectId));
        }

        Id = Guid.NewGuid();
        ObjectId = objectId;
        DisplayName = displayName;
        Email = email;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    /// <summary>Azure AD Object ID (oid claim).</summary>
    public string ObjectId { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<AppRole> Roles => _roles;

    public void UpdateProfile(string displayName, string email)
    {
        DisplayName = displayName;
        Email = email;
    }

    public void AssignRole(AppRole role)
    {
        if (_roles.Any(r => r.Id == role.Id))
        {
            return;
        }

        _roles.Add(role);
    }

    public void RevokeRole(AppRole role)
    {
        var existing = _roles.FirstOrDefault(r => r.Id == role.Id);
        if (existing is not null)
        {
            _roles.Remove(existing);
        }
    }
}
