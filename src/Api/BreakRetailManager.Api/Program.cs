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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("Cashier", policy => policy.RequireRole("Admin", "Manager", "Cashier"));
});

builder.Services.AddModules(builder.Configuration,
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
    await salesDb.Database.MigrateAsync();

    var usersDb = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
    await usersDb.Database.MigrateAsync();

    var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await inventoryDb.Database.MigrateAsync();
}

await UserManagementModule.SeedAsync(app.Services);

app.UseHttpsRedirection();
app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { name = "BreakRetailManager API" }))
    .AllowAnonymous();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapModules();
app.MapHub<InventoryHub>(InventoryHub.HubPath)
    .RequireAuthorization("Manager");

app.Run();
