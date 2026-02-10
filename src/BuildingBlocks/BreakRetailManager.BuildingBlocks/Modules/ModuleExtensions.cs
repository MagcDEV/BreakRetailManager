using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BreakRetailManager.BuildingBlocks.Modules;

public static class ModuleExtensions
{
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        var modules = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => typeof(IModule).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
            .Select(type => (IModule)Activator.CreateInstance(type.AsType())!)
            .ToArray();

        foreach (var module in modules)
        {
            services.AddSingleton(typeof(IModule), module);
            module.RegisterServices(services, configuration);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapModules(this IEndpointRouteBuilder endpoints)
    {
        var modules = endpoints.ServiceProvider.GetRequiredService<IEnumerable<IModule>>();

        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
