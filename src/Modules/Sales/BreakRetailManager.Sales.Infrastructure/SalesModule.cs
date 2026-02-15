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
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<SalesOrderService>();
        services.AddScoped<OfferService>();

        // ARCA fiscal services
        services.Configure<ArcaSettings>(configuration.GetSection(ArcaSettings.SectionName));

        var certSource = configuration.GetSection(ArcaSettings.SectionName)["CertificateSource"] ?? "File";
        if (certSource.Equals("KeyVault", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICertificateProvider, KeyVaultCertificateProvider>();
        }
        else
        {
            services.AddSingleton<ICertificateProvider, FileCertificateProvider>();
        }

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
                    catch (ArgumentException ex)
                    {
                        logger.LogWarning(ex, "Invalid sales order request");
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["request"] = new[] { ex.Message }
                        });
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogError(ex, "Fiscal authorization failed during sales order creation");
                        return Results.Problem(
                            detail: ex.Message,
                            title: "Fiscal authorization failed",
                            statusCode: 502);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Unexpected error while creating sales order");
                        return Results.Problem(
                            title: "Sales order creation failed",
                            statusCode: 500);
                    }
                })
            .Produces<SalesOrderDto>()
            .ProducesValidationProblem(400)
            .ProducesProblem(502);

        group.MapGet("/offers", async (OfferService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetOffersAsync(cancellationToken)))
            .Produces<IReadOnlyList<OfferDto>>()
            .RequireAuthorization("Manager");

        group.MapGet("/offers/active", async (OfferService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetActiveOffersAsync(cancellationToken)))
            .Produces<IReadOnlyList<OfferDto>>();

        group.MapGet("/offers/{id:guid}", async (Guid id, OfferService service, CancellationToken cancellationToken) =>
        {
            var offer = await service.GetByIdAsync(id, cancellationToken);
            return offer is null ? Results.NotFound() : Results.Ok(offer);
        })
            .Produces<OfferDto>()
            .RequireAuthorization("Manager");

        group.MapPost(
                "/offers",
                async (CreateOfferRequest request, OfferService service, ILogger<SalesModule> logger, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var created = await service.CreateOfferAsync(request, cancellationToken);
                        return Results.Created($"/api/sales/offers/{created.Id}", created);
                    }
                    catch (ArgumentException ex)
                    {
                        logger.LogWarning(ex, "Invalid offer create request");
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["request"] = new[] { ex.Message }
                        });
                    }
                })
            .Produces<OfferDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(400)
            .RequireAuthorization("Manager");

        group.MapPut(
                "/offers/{id:guid}",
                async (Guid id, UpdateOfferRequest request, OfferService service, ILogger<SalesModule> logger, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var updated = await service.UpdateOfferAsync(id, request, cancellationToken);
                        return updated is null ? Results.NotFound() : Results.Ok(updated);
                    }
                    catch (ArgumentException ex)
                    {
                        logger.LogWarning(ex, "Invalid offer update request for {OfferId}", id);
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["request"] = new[] { ex.Message }
                        });
                    }
                })
            .Produces<OfferDto>()
            .ProducesValidationProblem(400)
            .RequireAuthorization("Manager");

        group.MapDelete("/offers/{id:guid}", async (Guid id, OfferService service, CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteOfferAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization("Manager");

        group.MapPost("/offers/{id:guid}/activate", async (Guid id, OfferService service, CancellationToken cancellationToken) =>
        {
            var offer = await service.ActivateOfferAsync(id, cancellationToken);
            return offer is null ? Results.NotFound() : Results.Ok(offer);
        })
            .Produces<OfferDto>()
            .RequireAuthorization("Manager");

        group.MapPost("/offers/{id:guid}/deactivate", async (Guid id, OfferService service, CancellationToken cancellationToken) =>
        {
            var offer = await service.DeactivateOfferAsync(id, cancellationToken);
            return offer is null ? Results.NotFound() : Results.Ok(offer);
        })
            .Produces<OfferDto>()
            .RequireAuthorization("Manager");
    }
}
