using Delify.Modules.Catalog.Domain;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Admin.Application;
using Delify.Modules.Identity.Domain;
using Delify.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Admin.Endpoints;

internal static class AdminEstabelecimentosEndpoints
{
    private record CreateEstabelecimentoRequest(
        string Name,
        string Slug,
        string OwnerName,
        string OwnerEmail,
        string OwnerPassword);

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

        group.MapPost("/", async (
            CreateEstabelecimentoRequest req,
            IdentityDbContext identityDb,
            CatalogDbContext catalogDb,
            UserManager<AppUser> userManager) =>
        {
            var slug = req.Slug.Trim().ToLowerInvariant();

            if (await identityDb.Tenants.AnyAsync(t => t.Slug == slug))
                return Results.Conflict(new { error = "Slug já está em uso." });

            if (await userManager.FindByEmailAsync(req.OwnerEmail) is not null)
                return Results.Conflict(new { error = "E-mail já cadastrado." });

            var tenant = Tenant.Create(slug, req.Name.Trim());
            identityDb.Tenants.Add(tenant);
            await identityDb.SaveChangesAsync();

            var establishment = Establishment.Create(tenant.Id, slug, req.Name.Trim());
            catalogDb.Establishments.Add(establishment);
            await catalogDb.SaveChangesAsync();

            var owner = new AppUser
            {
                UserName = req.OwnerEmail,
                Email = req.OwnerEmail,
                FullName = req.OwnerName.Trim(),
                TenantId = tenant.Id,
                IsSuperAdmin = false,
            };

            var result = await userManager.CreateAsync(owner, req.OwnerPassword);
            if (!result.Succeeded)
            {
                // Rollback catalog and identity rows
                catalogDb.Establishments.Remove(establishment);
                await catalogDb.SaveChangesAsync();
                identityDb.Tenants.Remove(tenant);
                await identityDb.SaveChangesAsync();
                return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            return Results.Created(
                $"/admin/estabelecimentos/{tenant.Id}",
                new { id = tenant.Id, slug = tenant.Slug, name = tenant.Name, ownerEmail = owner.Email });
        })
        .WithName("AdminCreateEstabelecimento");

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
