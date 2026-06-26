using Delify.Modules.Admin.Application;
using Delify.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Admin.Endpoints;

internal static class AdminEstabelecimentosEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/estabelecimentos")
                       .WithTags("Admin")
                       .RequireAuthorization("SuperAdmin");

        group.MapGet("/", async (AdminQueryService svc) =>
        {
            var list = await svc.GetEstabelecimentosAsync();
            return Results.Ok(list);
        })
        .WithName("AdminListEstabelecimentos");

        group.MapGet("/{id:guid}", async (Guid id, AdminQueryService svc) =>
        {
            var detail = await svc.GetEstabelecimentoDetailAsync(id);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        })
        .WithName("AdminGetEstabelecimento");

        group.MapPatch("/{id:guid}/status", async (Guid id, IdentityDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null) return Results.NotFound();

            if (tenant.IsActive) tenant.Deactivate();
            else tenant.Activate();

            await db.SaveChangesAsync();
            return Results.Ok(new { id = tenant.Id, isActive = tenant.IsActive });
        })
        .WithName("AdminToggleEstabelecimentoStatus");

        return app;
    }
}
