using Delify.Modules.Admin.Application;
using Delify.Modules.Admin.Endpoints;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Admin;

public sealed class AdminModule : IModule
{
    public string Name => "Admin";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization(opts =>
            opts.AddPolicy("SuperAdmin", p => p.RequireClaim("is_super_admin", "true")));

        services.AddScoped<AdminQueryService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminAuthEndpoints.Map(endpoints);
        AdminEstabelecimentosEndpoints.Map(endpoints);
        return endpoints;
    }
}
