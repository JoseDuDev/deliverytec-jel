using Delify.Modules.Bff.Models;
using Delify.Modules.Catalog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Bff.Endpoints;

internal static class MenuEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/menu/{slug}", async (string slug, CatalogDbContext db) =>
        {
            var establishment = await db.Establishments
                .Include(e => e.Categories.OrderBy(c => c.Order))
                    .ThenInclude(c => c.Products)
                        .ThenInclude(p => p.Complements)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Slug == slug);

            if (establishment is null)
                return Results.NotFound();

            var response = new MenuResponse(
                establishment.Id,
                establishment.Name,
                establishment.Slug,
                establishment.Categories
                    .Select(c => new MenuCategoryDto(
                        c.Id,
                        c.Name,
                        c.Order,
                        c.Products
                            .Select(p => new MenuProductDto(
                                p.Id,
                                p.Name,
                                p.Price,
                                p.Description,
                                p.PhotoUrl,
                                p.Complements
                                    .Select(cp => new MenuComplementDto(cp.Id, cp.Name, cp.AdditionalPrice))
                                    .ToList()))
                            .ToList()))
                    .ToList());

            return Results.Ok(response);
        })
        .WithName("BffGetMenu")
        .WithTags("BFF")
        .AllowAnonymous();

        return app;
    }
}
