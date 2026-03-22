using System.Security.Claims;
using BreakRetailManager.UserManagement.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BreakRetailManager.UserManagement.Infrastructure.Authorization;

/// <summary>
/// Transforms the incoming ClaimsPrincipal by looking up the user's locally-stored
/// roles and adding them as role claims. This allows standard [Authorize(Roles = "Admin")]
/// to work with database-managed roles.
/// </summary>
public sealed class LocalRoleClaimsTransformation : IClaimsTransformation
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public LocalRoleClaimsTransformation(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
        {
            return principal;
        }

        // Azure AD uses "http://schemas.microsoft.com/identity/claims/objectidentifier" for oid
        var objectId = identity.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                       ?? identity.FindFirst("oid")?.Value;

        if (string.IsNullOrEmpty(objectId))
        {
            return principal;
        }

        // Avoid adding duplicate role claims on repeated calls
        if (identity.HasClaim(c => c.Type == identity.RoleClaimType && c.Issuer == "LocalRoles"))
        {
            return principal;
        }

        var cacheKey = $"user-roles:{objectId}";

        if (!_cache.TryGetValue(cacheKey, out string[]? roleNames))
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await repository.GetByObjectIdAsync(objectId);

            if (user is null)
            {
                return principal;
            }

            roleNames = user.Roles.Select(r => r.Name).ToArray();
            _cache.Set(cacheKey, roleNames, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });
        }

        foreach (var roleName in roleNames!)
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, roleName, ClaimValueTypes.String, "LocalRoles"));
        }

        return principal;
    }
}
