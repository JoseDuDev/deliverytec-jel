using Delify.Modules.Delivery.Abstractions;
using Delify.Modules.Delivery.Endpoints;
using Delify.Modules.Delivery.Infrastructure;
using Delify.Modules.Delivery.Providers;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Delivery;

public sealed class DeliveryModule : IModule
{
    public string Name => "Delivery";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DeliveryDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Delify"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "delivery")));

        services.AddHttpClient<LalamoveDeliveryProvider>(client =>
        {
            client.BaseAddress = new Uri(configuration["Lalamove:BaseUrl"] ?? "https://rest.lalamove.com");
            client.DefaultRequestHeaders.Add("Authorization", $"hmac {configuration["Lalamove:ApiKey"]}");
        });

        services.AddHttpClient<BorzoDeliveryProvider>(client =>
        {
            client.BaseAddress = new Uri(configuration["Borzo:BaseUrl"] ?? "https://api.borzodelivery.com");
            client.DefaultRequestHeaders.Add("X-DV-Auth-Token", configuration["Borzo:ApiKey"] ?? string.Empty);
        });

        services.AddScoped<IDeliveryProvider, LalamoveDeliveryProvider>();
        services.AddScoped<IDeliveryProvider, BorzoDeliveryProvider>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        DeliveryEndpoints.Map(endpoints);
        return endpoints;
    }
}
