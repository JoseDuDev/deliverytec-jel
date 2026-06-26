using Delify.Modules.Painel.Application;
using Delify.Modules.Painel.Endpoints;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Painel;

public sealed class PainelModule : IModule
{
    public string Name => "Painel";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<PainelQueryService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        PainelAuthEndpoints.Map(endpoints);
        PainelDashboardEndpoints.Map(endpoints);
        return endpoints;
    }
}
