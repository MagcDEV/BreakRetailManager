using System.Security.Claims;
using BreakRetailManager.UserManagement.Application;
using Microsoft.AspNetCore.Authentication;
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

    public LocalRoleClaimsTransformation(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await repository.GetByObjectIdAsync(objectId);

        if (user is null)
        {
            return principal;
        }

        foreach (var role in user.Roles)
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, role.Name, ClaimValueTypes.String, "LocalRoles"));
        }

        return principal;
    }
}
