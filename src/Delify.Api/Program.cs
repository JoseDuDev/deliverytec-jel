using Delify.Modules.Catalog;
using Delify.Modules.Delivery;
using Delify.Modules.Identity;
using Delify.Modules.Orders;
using Delify.Modules.Payments;
using Delify.Shared.Abstractions;
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
