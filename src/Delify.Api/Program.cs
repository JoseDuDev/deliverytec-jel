using Delify.Modules.Admin;
using Delify.Modules.Bff;
using Delify.Modules.Painel;
using Delify.Modules.Catalog;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Delivery;
using Delify.Modules.Delivery.Infrastructure;
using Delify.Modules.Dinein;
using Delify.Modules.Dinein.Infrastructure;
using Delify.Modules.Identity;
using Delify.Modules.Identity.Infrastructure;
using Delify.Modules.Orders;
using Delify.Modules.Orders.Infrastructure;
using Delify.Modules.Payments;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.Abstractions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Null impls registradas por padrão; os módulos sobrescrevem com as reais
builder.Services.AddSingleton<IOrderTrackingNotifier, NullOrderTrackingNotifier>();
builder.Services.AddSingleton<IPainelDashboardNotifier, NullPainelDashboardNotifier>();

var modules = new List<IModule>
{
    new IdentityModule(),
    new AdminModule(),
    new PainelModule(),
    new CatalogModule(),
    new OrdersModule(),
    new PaymentsModule(),
    new DeliveryModule(),
    new DineinModule(),
    new BffModule()          // BFF por último para override do IOrderTrackingNotifier
};

foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(OrdersModule).Assembly);
    x.AddConsumers(typeof(PaymentsModule).Assembly);
    x.AddConsumers(typeof(PainelModule).Assembly);

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var vhost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
        var port = builder.Configuration.GetValue<ushort>("RabbitMQ:Port", 5672);

        cfg.Host(host, port, vhost, h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "delify");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "delify");
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddOpenApi();
builder.Services.AddOutputCache();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply pending EF migrations on startup
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await sp.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<DeliveryDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<DineinDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

foreach (var module in modules)
    module.MapEndpoints(app);

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithTags("Health")
   .AllowAnonymous();

app.Run();

public partial class Program { }
