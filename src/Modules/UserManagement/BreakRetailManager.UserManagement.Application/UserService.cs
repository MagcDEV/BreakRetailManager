using BreakRetailManager.UserManagement.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace BreakRetailManager.UserManagement.Application;

public sealed class UserService
{
    private const string RolesCacheKey = "roles-all";

    private readonly IUserRepository _repository;
    private readonly IMemoryCache _cache;

    public UserService(IUserRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.GetAllAsync(cancellationToken);
        return users.Select(UserMappings.ToDto).ToList();
    }

    public async Task<UserDto> GetOrProvisionCurrentUserAsync(
        string objectId,
        string displayName,
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByObjectIdAsync(objectId, cancellationToken);

        if (user is not null)
        {
            user.UpdateProfile(displayName, email);
            await _repository.SaveChangesAsync(cancellationToken);
            return UserMappings.ToDto(user);
        }

        // Auto-provision new user
        user = new Domain.Entities.AppUser(objectId, displayName, email);

        // First user ever becomes Admin
        var anyExist = await _repository.AnyUsersExistAsync(cancellationToken);
        if (!anyExist)
        {
            var adminRole = await _repository.GetRoleByNameAsync("Admin", cancellationToken);
            if (adminRole is not null)
            {
                user.AssignRole(adminRole);
            }
        }

        await _repository.AddAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return UserMappings.ToDto(user);
    }

    public async Task<UserDto?> AssignRoleAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var role = await _repository.GetRoleByNameAsync(roleName, cancellationToken);
        if (role is null)
        {
            throw new ArgumentException($"Role '{roleName}' does not exist.");
        }

        user.AssignRole(role);
        await _repository.SaveChangesAsync(cancellationToken);

        return UserMappings.ToDto(user);
    }

    public async Task<UserDto?> RevokeRoleAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var role = await _repository.GetRoleByNameAsync(roleName, cancellationToken);
        if (role is null)
        {
            throw new ArgumentException($"Role '{roleName}' does not exist.");
        }

        user.RevokeRole(role);
        await _repository.SaveChangesAsync(cancellationToken);

        return UserMappings.ToDto(user);
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetOrCreateAsync(RolesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var roles = await _repository.GetAllRolesAsync(cancellationToken);
            return roles.Select(UserMappings.ToDto).ToList() as IReadOnlyList<RoleDto>;
        });

        return cached ?? [];
    }
}
