namespace BreakRetailManager.UserManagement.Contracts;

public sealed record UserDto(
    Guid Id,
    string ObjectId,
    string DisplayName,
    string Email,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Roles);
