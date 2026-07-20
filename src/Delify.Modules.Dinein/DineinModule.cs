using Delify.Modules.Dinein.Endpoints;
using Delify.Modules.Dinein.Infrastructure;
using Delify.Modules.Dinein.Services;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Dinein;

public sealed class DineinModule : IModule
{
    public string Name => "Dinein";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DineinDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Delify"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "dinein")));

        services.AddSingleton<MesaNotifier>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        DineinPainelEndpoints.Map(endpoints);
        DineinMesaEndpoints.Map(endpoints);
        return endpoints;
    }
}
