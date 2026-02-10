using BreakRetailManager.UserManagement.Application;
using BreakRetailManager.UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakRetailManager.UserManagement.Infrastructure.Data;

public sealed class UserRepository : IUserRepository
{
    private readonly UserManagementDbContext _dbContext;

    public UserRepository(UserManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AppUser?> GetByObjectIdAsync(string objectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.ObjectId == objectId, cancellationToken);
    }

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<AppRole?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
    }

    public async Task<IReadOnlyList<AppRole>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
