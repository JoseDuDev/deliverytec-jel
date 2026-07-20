using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Painel.Application;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Painel.Endpoints;

internal record UpdateEstablishmentRequest(
    string Name, string? Description, string? LogoUrl, decimal DeliveryFee,
    bool ServiceFeeEnabled = true, decimal ServiceFeePercent = 10m);

internal static class PainelDashboardEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/painel")
            .RequireAuthorization()
            .WithTags("Painel");

        group.MapGet("/me", async (ITenantContext tenant, PainelQueryService svc) =>
        {
            var result = await svc.GetDashboardAsync(tenant.TenantId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPatch("/me/status", async (ITenantContext tenant, CatalogDbContext db) =>
        {
            var est = await db.Establishments
                .FirstOrDefaultAsync(e => e.TenantId == tenant.TenantId);

            if (est is null) return Results.NotFound();

            est.Update(est.Name, est.Description, est.LogoUrl, !est.IsOpen, est.DeliveryFee);
            await db.SaveChangesAsync();

            return Results.Ok(new { est.Id, est.IsOpen });
        });

        group.MapPatch("/me", async (ITenantContext tenant, CatalogDbContext db, UpdateEstablishmentRequest req) =>
        {
            var est = await db.Establishments
                .FirstOrDefaultAsync(e => e.TenantId == tenant.TenantId);

            if (est is null) return Results.NotFound();

            est.Update(req.Name, req.Description, req.LogoUrl, est.IsOpen, req.DeliveryFee);
            est.SetServiceFee(req.ServiceFeeEnabled, req.ServiceFeePercent);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                est.Id, est.Name, est.Description, est.LogoUrl, est.IsOpen, est.DeliveryFee,
                est.ServiceFeeEnabled, est.ServiceFeePercent
            });
        });

        return app;
    }
}
