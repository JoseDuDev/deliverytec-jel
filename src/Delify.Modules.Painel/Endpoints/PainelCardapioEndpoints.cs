using Delify.Modules.Catalog.Domain;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Painel.Endpoints;

internal static class PainelCardapioEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/painel/cardapio")
            .RequireAuthorization()
            .WithTags("Painel");

        // ── Cardápio completo ───────────────────────────────────────────
        group.MapGet("/", async (ITenantContext tenant, CatalogDbContext db) =>
        {
            var est = await db.Establishments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.TenantId == tenant.TenantId);

            if (est is null) return Results.NotFound();

            var categories = await db.Categories
                .AsNoTracking()
                .Include(c => c.Products)
                .Where(c => c.TenantId == tenant.TenantId)
                .OrderBy(c => c.Order).ThenBy(c => c.Name)
                .ToListAsync();

            return Results.Ok(new CardapioResponse(
                est.Id,
                categories.Select(c => new CategoryResponse(
                    c.Id, c.EstablishmentId, c.Name, c.Order, c.IsActive,
                    c.Products
                        .OrderBy(p => p.Name)
                        .Select(p => new ProductResponse(
                            p.Id, p.CategoryId, p.Name, p.Description,
                            p.Price, p.PhotoUrl, p.IsAvailable,
                            p.IsFeatured, p.FeaturedOrder))))));
        });

        // ── Categorias ──────────────────────────────────────────────────
        group.MapPost("/categorias", async (
            ITenantContext tenant, CatalogDbContext db, CreateCategoryReq req) =>
        {
            var est = await db.Establishments
                .FirstOrDefaultAsync(e => e.TenantId == tenant.TenantId);

            if (est is null) return Results.NotFound();

            var cat = Category.Create(tenant.TenantId, est.Id, req.Name, req.Order);
            db.Categories.Add(cat);
            await db.SaveChangesAsync();

            return Results.Created($"/painel/cardapio/categorias/{cat.Id}",
                new CategoryResponse(cat.Id, cat.EstablishmentId, cat.Name, cat.Order, cat.IsActive, []));
        });

        group.MapPatch("/categorias/{id:guid}", async (
            Guid id, ITenantContext tenant, CatalogDbContext db, UpdateCategoryReq req) =>
        {
            var cat = await db.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.TenantId);

            if (cat is null) return Results.NotFound();

            cat.Update(req.Name, req.Order, req.IsActive);
            await db.SaveChangesAsync();

            return Results.Ok(new CategoryResponse(
                cat.Id, cat.EstablishmentId, cat.Name, cat.Order, cat.IsActive, []));
        });

        group.MapDelete("/categorias/{id:guid}", async (
            Guid id, ITenantContext tenant, CatalogDbContext db) =>
        {
            var cat = await db.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.TenantId);

            if (cat is null) return Results.NotFound();
            if (cat.Products.Any())
                return Results.Conflict("Remova os produtos desta categoria antes de excluí-la.");

            db.Categories.Remove(cat);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ── Produtos ────────────────────────────────────────────────────
        group.MapPost("/categorias/{id:guid}/produtos", async (
            Guid id, ITenantContext tenant, CatalogDbContext db, CreateProductReq req) =>
        {
            var cat = await db.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.TenantId);

            if (cat is null) return Results.NotFound();

            var product = Product.Create(tenant.TenantId, id, req.Name, req.Price, req.Description, req.PhotoUrl);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            return Results.Created($"/painel/cardapio/produtos/{product.Id}",
                new ProductResponse(product.Id, product.CategoryId, product.Name,
                    product.Description, product.Price, product.PhotoUrl, product.IsAvailable,
                    product.IsFeatured, product.FeaturedOrder));
        });

        group.MapPatch("/produtos/{id:guid}", async (
            Guid id, ITenantContext tenant, CatalogDbContext db, UpdateProductReq req) =>
        {
            var product = await db.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenant.TenantId);

            if (product is null) return Results.NotFound();

            product.Update(req.Name, req.Description, req.Price, req.PhotoUrl, req.IsAvailable,
                req.IsFeatured, req.FeaturedOrder);
            await db.SaveChangesAsync();

            return Results.Ok(new ProductResponse(product.Id, product.CategoryId, product.Name,
                product.Description, product.Price, product.PhotoUrl, product.IsAvailable,
                product.IsFeatured, product.FeaturedOrder));
        });

        group.MapDelete("/produtos/{id:guid}", async (
            Guid id, ITenantContext tenant, CatalogDbContext db) =>
        {
            var product = await db.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenant.TenantId);

            if (product is null) return Results.NotFound();

            db.Products.Remove(product);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    private record CreateCategoryReq(string Name, int Order = 0);
    private record UpdateCategoryReq(string Name, int Order, bool IsActive);
    private record CreateProductReq(string Name, decimal Price, string? Description = null, string? PhotoUrl = null);
    private record UpdateProductReq(
        string Name, decimal Price, bool IsAvailable,
        string? Description = null, string? PhotoUrl = null,
        bool IsFeatured = false, int FeaturedOrder = 0);
}

internal record ProductResponse(
    Guid Id, Guid CategoryId, string Name, string? Description, decimal Price,
    string? PhotoUrl, bool IsAvailable, bool IsFeatured, int FeaturedOrder);
internal record CategoryResponse(Guid Id, Guid EstablishmentId, string Name, int Order, bool IsActive, IEnumerable<ProductResponse> Products);
internal record CardapioResponse(Guid EstId, IEnumerable<CategoryResponse> Categories);
