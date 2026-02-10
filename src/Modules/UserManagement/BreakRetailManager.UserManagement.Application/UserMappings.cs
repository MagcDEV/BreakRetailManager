using BreakRetailManager.UserManagement.Contracts;
using BreakRetailManager.UserManagement.Domain.Entities;

namespace BreakRetailManager.UserManagement.Application;

public static class UserMappings
{
    public static UserDto ToDto(AppUser user)
    {
        return new UserDto(
            user.Id,
            user.ObjectId,
            user.DisplayName,
            user.Email,
            user.CreatedAt,
            user.Roles.Select(r => r.Name).ToList());
    }

    public static RoleDto ToDto(AppRole role)
    {
        return new RoleDto(role.Id, role.Name);
    }
}
