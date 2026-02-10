using System.Security.Claims;
using BreakRetailManager.BuildingBlocks.Modules;
using BreakRetailManager.UserManagement.Application;
using BreakRetailManager.UserManagement.Contracts;
using BreakRetailManager.UserManagement.Domain.Entities;
using BreakRetailManager.UserManagement.Infrastructure.Authorization;
using BreakRetailManager.UserManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BreakRetailManager.UserManagement.Infrastructure;

public sealed class UserManagementModule : IModule
{
    public string Name => "UserManagement";

    private static readonly string[] SeedRoles = ["Admin", "Manager", "Cashier"];

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");
        }

        services.AddDbContext<UserManagementDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<UserService>();
        services.AddTransient<IClaimsTransformation, LocalRoleClaimsTransformation>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // Current user profile — auto-provisions on first call
        group.MapGet("/me", async (HttpContext httpContext, UserService service, CancellationToken cancellationToken) =>
        {
            var (objectId, displayName, email) = ExtractUserClaims(httpContext);
            if (objectId is null)
            {
                return Results.Unauthorized();
            }

            var user = await service.GetOrProvisionCurrentUserAsync(objectId, displayName, email, cancellationToken);
            return Results.Ok(user);
        }).Produces<UserDto>();

        // List all users — Admin only
        group.MapGet("/", async (UserService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllUsersAsync(cancellationToken)))
            .Produces<IReadOnlyList<UserDto>>()
            .RequireAuthorization("Admin");

        // List all available roles
        group.MapGet("/roles", async (UserService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllRolesAsync(cancellationToken)))
            .Produces<IReadOnlyList<RoleDto>>()
            .RequireAuthorization("Admin");

        // Assign a role to a user — Admin only
        group.MapPost("/{userId:guid}/roles", async (
            Guid userId,
            AssignRoleRequest request,
            UserService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AssignRoleAsync(userId, request.RoleName, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .Produces<UserDto>()
        .RequireAuthorization("Admin");

        // Revoke a role from a user — Admin only
        group.MapDelete("/{userId:guid}/roles/{roleName}", async (
            Guid userId,
            string roleName,
            UserService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RevokeRoleAsync(userId, roleName, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .Produces<UserDto>()
        .RequireAuthorization("Admin");
    }

    /// <summary>Seed the database with default roles.</summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();

        var existingRoles = await dbContext.Roles.Select(r => r.Name).ToListAsync();
        foreach (var roleName in SeedRoles)
        {
            if (!existingRoles.Contains(roleName))
            {
                dbContext.Roles.Add(new AppRole(roleName));
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static (string? ObjectId, string DisplayName, string Email) ExtractUserClaims(HttpContext httpContext)
    {
        var identity = httpContext.User.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
        {
            return (null, string.Empty, string.Empty);
        }

        var objectId = identity.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                       ?? identity.FindFirst("oid")?.Value;
        var displayName = identity.FindFirst("name")?.Value ?? string.Empty;
        var email = identity.FindFirst("preferred_username")?.Value
                    ?? identity.FindFirst(ClaimTypes.Email)?.Value
                    ?? string.Empty;

        return (objectId, displayName, email);
    }
}
