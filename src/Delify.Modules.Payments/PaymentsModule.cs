using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Endpoints;
using Delify.Modules.Payments.Gateways;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Payments;

public sealed class PaymentsModule : IModule
{
    public string Name => "Payments";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentsDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Delify"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "payments")));

        services.AddHttpClient<IPaymentGateway, AsaasPaymentGateway>(client =>
        {
            client.BaseAddress = new Uri(configuration["Asaas:BaseUrl"]
                ?? "https://sandbox.asaas.com");
            client.DefaultRequestHeaders.Add("access_token",
                configuration["Asaas:ApiKey"] ?? string.Empty);
        });

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        CheckoutEndpoint.Map(endpoints);
        WebhookEndpoint.Map(endpoints);
        return endpoints;
    }
}
