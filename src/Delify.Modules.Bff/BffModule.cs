using Delify.Modules.Bff.Endpoints;
using Delify.Modules.Bff.Services;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Delify.Modules.Bff;

public sealed class BffModule : IModule
{
    public string Name => "Bff";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Substitui o NullOrderTrackingNotifier registrado em Program.cs
        services.Replace(ServiceDescriptor.Singleton<IOrderTrackingNotifier, OrderTrackingService>());
        // Registra também como OrderTrackingService para o TrackingEndpoints poder injetá-lo diretamente
        services.AddSingleton(sp => (OrderTrackingService)sp.GetRequiredService<IOrderTrackingNotifier>());

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        MenuEndpoints.Map(endpoints);
        AuthEndpoints.Map(endpoints);
        OrderEndpoints.Map(endpoints);
        TrackingEndpoints.Map(endpoints);
        DevEndpoints.Map(endpoints);
        return endpoints;
    }
}
