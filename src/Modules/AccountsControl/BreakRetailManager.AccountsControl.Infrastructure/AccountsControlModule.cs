using System.Security.Claims;
using BreakRetailManager.AccountsControl.Application;
using BreakRetailManager.AccountsControl.Contracts;
using BreakRetailManager.AccountsControl.Infrastructure.Data;
using BreakRetailManager.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BreakRetailManager.AccountsControl.Infrastructure;

public sealed class AccountsControlModule : IModule
{
    public string Name => "AccountsControl";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");
        }

        services.AddDbContext<AccountsControlDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

        services.AddScoped<IAccountsRepository, AccountsRepository>();
        services.AddScoped<AccountsService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/accounts")
            .WithTags("Accounts");

        group.MapGet("/summary", async (AccountsService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPublicSummaryAsync(cancellationToken)))
            .Produces<PublicSummaryDto>()
            .CacheOutput("Short")
            .AllowAnonymous();

        group.MapGet("/employees", async (AccountsService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetActiveAccountsByTypeAsync(AccountType.Employee, cancellationToken)))
            .Produces<IReadOnlyList<AccountOptionDto>>()
            .CacheOutput("Medium")
            .AllowAnonymous();

        group.MapGet("/expenses", async (AccountsService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetActiveAccountsByTypeAsync(AccountType.GeneralExpense, cancellationToken)))
            .Produces<IReadOnlyList<AccountOptionDto>>()
            .CacheOutput("Medium")
            .AllowAnonymous();

        group.MapGet("/employees/{accountId:guid}", async (Guid accountId, AccountsService service, CancellationToken cancellationToken) =>
        {
            var account = await service.GetEmployeeAccountSummaryAsync(accountId, cancellationToken);
            return account is null ? Results.NotFound() : Results.Ok(account);
        })
            .Produces<AccountSummaryDto>()
            .AllowAnonymous();

        group.MapGet("/employees/{accountId:guid}/movements", async (Guid accountId, AccountsService service, CancellationToken cancellationToken) =>
        {
            var account = await service.GetEmployeeAccountSummaryAsync(accountId, cancellationToken);
            if (account is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(await service.GetEmployeeMovementsAsync(accountId, cancellationToken));
        })
            .Produces<IReadOnlyList<MovementDto>>()
            .AllowAnonymous();

        group.MapPost("/employees/{accountId:guid}/movements", async (
            Guid accountId,
            CreateMovementRequest request,
            AccountsService service,
            ILogger<AccountsControlModule> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var movement = await service.CreateEmployeeMovementAsync(accountId, request, cancellationToken);
                return movement is null
                    ? Results.NotFound()
                    : Results.Created($"/api/accounts/employees/{accountId}/movements/{movement.Id}", movement);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                logger.LogWarning(ex, "Out of range employee movement request for account {AccountId}", accountId);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [ex.ParamName ?? "amount"] = [ex.Message]
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid employee movement request for account {AccountId}", accountId);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [ex.Message]
                });
            }
        })
            .Produces<MovementDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(400)
            .AllowAnonymous();

        group.MapGet("/expenses/{accountId:guid}", async (Guid accountId, AccountsService service, CancellationToken cancellationToken) =>
        {
            var account = await service.GetExpenseAccountSummaryAsync(accountId, cancellationToken);
            return account is null ? Results.NotFound() : Results.Ok(account);
        })
            .Produces<AccountSummaryDto>()
            .AllowAnonymous();

        group.MapGet("/expenses/{accountId:guid}/movements", async (Guid accountId, AccountsService service, CancellationToken cancellationToken) =>
        {
            var account = await service.GetExpenseAccountSummaryAsync(accountId, cancellationToken);
            if (account is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(await service.GetExpenseMovementsAsync(accountId, cancellationToken));
        })
            .Produces<IReadOnlyList<MovementDto>>()
            .AllowAnonymous();

        group.MapPost("/expenses/{accountId:guid}/movements", async (
            Guid accountId,
            CreateMovementRequest request,
            AccountsService service,
            ILogger<AccountsControlModule> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var movement = await service.CreateExpenseMovementAsync(accountId, request, cancellationToken);
                return movement is null
                    ? Results.NotFound()
                    : Results.Created($"/api/accounts/expenses/{accountId}/movements/{movement.Id}", movement);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                logger.LogWarning(ex, "Out of range expense movement request for account {AccountId}", accountId);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [ex.ParamName ?? "amount"] = [ex.Message]
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid expense movement request for account {AccountId}", accountId);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [ex.Message]
                });
            }
        })
            .Produces<MovementDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(400)
            .AllowAnonymous();

        var adminGroup = group.MapGroup("/admin")
            .RequireAuthorization("Admin");

        adminGroup.MapGet("/dashboard", async (AccountsService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetDashboardAsync(cancellationToken)))
            .Produces<AdminDashboardDto>();

        adminGroup.MapGet("/accounts/{accountId:guid}", async (Guid accountId, AccountsService service, CancellationToken cancellationToken) =>
        {
            var account = await service.GetAccountDetailAsync(accountId, cancellationToken);
            return account is null ? Results.NotFound() : Results.Ok(account);
        })
            .Produces<AdminAccountDetailDto>();

        adminGroup.MapPost("/accounts", async (
            HttpContext httpContext,
            CreateAccountRequest request,
            AccountsService service,
            ILogger<AccountsControlModule> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var created = await service.CreateAccountAsync(request, ExtractObjectId(httpContext.User), cancellationToken);
                return Results.Created($"/api/accounts/admin/accounts/{created.Id}", created);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid account create request");
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [ex.Message]
                });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Duplicate or invalid account create request");
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = [ex.Message]
                });
            }
        })
            .Produces<AccountSummaryDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(400);

        adminGroup.MapDelete("/accounts/{accountId:guid}", async (
            Guid accountId,
            HttpContext httpContext,
            AccountsService service,
            CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeactivateAccountAsync(accountId, ExtractObjectId(httpContext.User), cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
            .Produces(StatusCodes.Status204NoContent);

        adminGroup.MapPost("/accounts/{accountId:guid}/adjustments", async (
            Guid accountId,
            HttpContext httpContext,
            CreateAdminAdjustmentRequest request,
            AccountsService service,
            ILogger<AccountsControlModule> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var created = await service.CreateAdminAdjustmentAsync(
                    accountId,
                    request,
                    ExtractObjectId(httpContext.User),
                    cancellationToken);

                return created is null
                    ? Results.NotFound()
                    : Results.Created($"/api/accounts/admin/accounts/{accountId}/adjustments/{created.Id}", created);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                logger.LogWarning(ex, "Out of range admin adjustment request for account {AccountId}", accountId);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [ex.ParamName ?? "amount"] = [ex.Message]
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid admin adjustment request for account {AccountId}", accountId);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [ex.Message]
                });
            }
        })
            .Produces<MovementDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(400);

        adminGroup.MapGet("/movements", async (
            int? page,
            int? pageSize,
            AccountsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAdminMovementsAsync(page ?? 1, pageSize ?? 25, cancellationToken);
            return Results.Ok(result);
        })
            .Produces<MovementPageDto>();
    }

    private static string? ExtractObjectId(ClaimsPrincipal user)
    {
        return (user.Identity as ClaimsIdentity)?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
               ?? (user.Identity as ClaimsIdentity)?.FindFirst("oid")?.Value;
    }
}
