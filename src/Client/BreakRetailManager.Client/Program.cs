using BreakRetailManager.Client;
using BreakRetailManager.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;
var apiScopes = builder.Configuration.GetSection("Api:Scopes").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

    // Popup mode can be blocked (or fail to complete) in some browsers/environments.
    // Redirect mode is more reliable for guest/external users.
    options.ProviderOptions.LoginMode = "redirect";
    options.ProviderOptions.Cache.StoreAuthStateInCookie = true;

    foreach (var scope in apiScopes)
    {
        options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
    }
});

builder.Services.AddHttpClient("ApiClient", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler(sp =>
    {
        var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
            .ConfigureHandler(authorizedUrls: new[] { apiBaseUrl }, scopes: apiScopes);
        return handler;
    });

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));
builder.Services.AddScoped<IndexedDbSalesStore>();
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<SalesApiClient>();
builder.Services.AddScoped<UserApiClient>();
builder.Services.AddScoped<InventoryApiClient>();
builder.Services.AddScoped<InventoryRealtimeClient>();
builder.Services.AddScoped<UserRoleProvider>();
builder.Services.AddScoped<CurrentLocationProvider>();

await builder.Build().RunAsync();
