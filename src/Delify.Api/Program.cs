using Delify.Modules.Catalog;
using Delify.Modules.Delivery;
using Delify.Modules.Identity;
using Delify.Modules.Orders;
using Delify.Modules.Payments;
using Delify.Shared.Abstractions;
using MassTransit;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var modules = new List<IModule>
{
    new IdentityModule(),
    new CatalogModule(),
    new OrdersModule(),
    new PaymentsModule(),
    new DeliveryModule()
};

foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(OrdersModule).Assembly);
    x.AddConsumers(typeof(PaymentsModule).Assembly);

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

builder.Services.AddOpenApi();
builder.Services.AddOutputCache();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

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
