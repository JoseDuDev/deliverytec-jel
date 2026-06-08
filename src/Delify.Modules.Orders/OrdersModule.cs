using Delify.Modules.Orders.Endpoints;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Orders;

public sealed class OrdersModule : IModule
{
    public string Name => "Orders";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrdersDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Delify"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "orders")));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(OrdersModule).Assembly));

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        OrderEndpoints.Map(endpoints);
        return endpoints;
    }
}
