using BreakRetailManager.AccountsControl.Infrastructure;
using BreakRetailManager.AccountsControl.Infrastructure.Data;
using BreakRetailManager.BuildingBlocks.Modules;
using BreakRetailManager.BuildingBlocks.Realtime;
using BreakRetailManager.Inventory.Infrastructure;
using BreakRetailManager.Inventory.Infrastructure.Data;
using BreakRetailManager.Sales.Infrastructure;
using BreakRetailManager.Sales.Infrastructure.Data;
using BreakRetailManager.UserManagement.Infrastructure;
using BreakRetailManager.UserManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.NoCache());
    options.AddPolicy("Products", builder => builder.Expire(TimeSpan.FromSeconds(30)).Tag("products"));
    options.AddPolicy("ActiveOffers", builder => builder.Expire(TimeSpan.FromSeconds(30)).Tag("active-offers"));
    options.AddPolicy("Providers", builder => builder.Expire(TimeSpan.FromSeconds(60)).Tag("providers"));
    options.AddPolicy("Locations", builder => builder.Expire(TimeSpan.FromSeconds(60)).Tag("locations"));
    options.AddPolicy("Roles", builder => builder.Expire(TimeSpan.FromSeconds(120)).Tag("roles"));
    options.AddPolicy("AccountsSummary", builder => builder.Expire(TimeSpan.FromSeconds(30)).Tag("accounts-summary"));
    options.AddPolicy("AccountsList", builder => builder.Expire(TimeSpan.FromSeconds(60)).Tag("accounts-list"));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("Cashier", policy => policy.RequireRole("Admin", "Manager", "Cashier"));
});

builder.Services.AddModules(builder.Configuration,
    typeof(AccountsControlModule).Assembly,
    typeof(SalesModule).Assembly,
    typeof(UserManagementModule).Assembly,
    typeof(InventoryModule).Assembly);

var allowedOrigins = builder.Configuration.GetSection("Client:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();

    var salesDb = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
    var usersDb = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
    var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    var accountsDb = scope.ServiceProvider.GetRequiredService<AccountsControlDbContext>();

    // The module contexts share one database, and the inventory migrations currently
    // include data fixes that touch sales tables, so migrate them in a deterministic order.
    await salesDb.Database.MigrateAsync();
    await usersDb.Database.MigrateAsync();
    await inventoryDb.Database.MigrateAsync();
    await accountsDb.Database.MigrateAsync();
}

await UserManagementModule.SeedAsync(app.Services);

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapGet("/", () => Results.Ok(new { name = "BreakRetailManager API" }))
    .AllowAnonymous();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapModules();
app.MapHub<InventoryHub>(InventoryHub.HubPath)
    .RequireAuthorization("Manager");

app.Run();
