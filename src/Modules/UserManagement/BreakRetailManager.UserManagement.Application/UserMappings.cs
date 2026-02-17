using System.Text;
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
            RepairText(user.DisplayName),
            RepairText(user.Email),
            user.CreatedAt,
            user.Roles.Select(r => RepairText(r.Name)).ToList());
    }

    public static RoleDto ToDto(AppRole role)
    {
        return new RoleDto(role.Id, RepairText(role.Name));
    }

    private static string RepairText(string value)
    {
        if (string.IsNullOrEmpty(value) || (!value.Contains('Ã') && !value.Contains('Â')))
        {
            return value;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] > 0xFF)
            {
                return value;
            }
        }

        var repaired = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(value));
        return repaired.Contains('\uFFFD') ? value : repaired;
    }
}
