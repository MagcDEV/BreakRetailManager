using BreakRetailManager.UserManagement.Domain.Entities;

namespace BreakRetailManager.UserManagement.Application;

public interface IUserRepository
{
    Task<AppUser?> GetByObjectIdAsync(string objectId, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AppRole?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppRole>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken = default);

    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
