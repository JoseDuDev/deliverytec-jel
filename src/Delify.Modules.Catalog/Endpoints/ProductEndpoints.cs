using Delify.Modules.Catalog.Domain;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Catalog.Endpoints;

internal static class ProductEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog");

        group.MapGet("/establishments/{slug}/menu", async (
            string slug,
            CatalogDbContext db) =>
        {
            var establishment = await db.Establishments
                .Include(e => e.Categories)
                    .ThenInclude(c => c.Products)
                        .ThenInclude(p => p.Complements)
                .FirstOrDefaultAsync(e => e.Slug == slug);

            return establishment is null
                ? Results.NotFound()
                : Results.Ok(establishment);
        })
        .WithName("GetMenu")
        .AllowAnonymous();

        group.MapPost("/establishments", async (
            CreateEstablishmentRequest req,
            ITenantContext tenant,
            CatalogDbContext db) =>
        {
            var establishment = Establishment.Create(tenant.TenantId, req.Slug, req.Name);
            db.Establishments.Add(establishment);
            await db.SaveChangesAsync();
            return Results.Created($"/api/catalog/establishments/{establishment.Slug}", establishment);
        })
        .WithName("CreateEstablishment")
        .RequireAuthorization();

        group.MapPost("/establishments/{id:guid}/categories", async (
            Guid id,
            CreateCategoryRequest req,
            ITenantContext tenant,
            CatalogDbContext db) =>
        {
            var category = Category.Create(tenant.TenantId, id, req.Name, req.Order);
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return Results.Created($"/api/catalog/categories/{category.Id}", category);
        })
        .WithName("CreateCategory")
        .RequireAuthorization();

        group.MapPost("/categories/{id:guid}/products", async (
            Guid id,
            CreateProductRequest req,
            ITenantContext tenant,
            CatalogDbContext db) =>
        {
            var product = Product.Create(tenant.TenantId, id, req.Name, req.Price, req.Description);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/catalog/products/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .RequireAuthorization();

        return app;
    }

    private record CreateEstablishmentRequest(string Slug, string Name);
    private record CreateCategoryRequest(string Name, int Order = 0);
    private record CreateProductRequest(string Name, decimal Price, string? Description = null);
}
