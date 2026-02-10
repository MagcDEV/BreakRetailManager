using BreakRetailManager.BuildingBlocks.Modules;
using BreakRetailManager.Sales.Application;
using BreakRetailManager.Sales.Contracts;
using BreakRetailManager.Sales.Infrastructure.Arca;
using BreakRetailManager.Sales.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BreakRetailManager.Sales.Infrastructure;

public sealed class SalesModule : IModule
{
    public string Name => "Sales";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");
        }

        services.AddDbContext<SalesDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<SalesOrderService>();

        // ARCA fiscal services
        services.Configure<ArcaSettings>(configuration.GetSection(ArcaSettings.SectionName));
        services.AddHttpClient<WsaaClient>();
        services.AddHttpClient<WsfeClient>();
        services.AddSingleton<WsaaClient>();
        services.AddScoped<WsfeClient>();
        services.AddScoped<IArcaFiscalService, ArcaFiscalService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/sales")
            .WithTags("Sales")
            .RequireAuthorization("Cashier");

        group.MapGet("/orders", async (SalesOrderService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetOrdersAsync(cancellationToken)))
            .Produces<IReadOnlyList<SalesOrderDto>>();

        group.MapPost(
                "/orders",
                async (CreateSalesOrderRequest request, SalesOrderService service,
                    ILogger<SalesModule> logger, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var order = await service.CreateOrderAsync(request, cancellationToken);
                        return Results.Ok(order);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to create sales order");
                        return Results.Problem(
                            detail: ex.Message,
                            title: "Fiscal authorization failed",
                            statusCode: 502);
                    }
                })
            .Produces<SalesOrderDto>()
            .ProducesProblem(502);
    }
}
