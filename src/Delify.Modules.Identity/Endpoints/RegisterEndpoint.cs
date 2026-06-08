using Delify.Modules.Identity.Domain;
using Delify.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Identity.Endpoints;

internal static class RegisterEndpoint
{
    internal record Request(string Slug, string EstablishmentName, string Email, string Password, string FullName);
    internal record Response(Guid TenantId, Guid UserId);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (
            Request req,
            IdentityDbContext db,
            UserManager<AppUser> userManager) =>
        {
            var existing = db.Tenants.FirstOrDefault(t => t.Slug == req.Slug.ToLowerInvariant());
            if (existing is not null)
                return Results.Conflict(new { error = "Slug already taken." });

            var tenant = Tenant.Create(req.Slug, req.EstablishmentName);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var user = new AppUser
            {
                UserName = req.Email,
                Email = req.Email,
                FullName = req.FullName,
                TenantId = tenant.Id
            };

            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors);

            return Results.Created($"/api/tenants/{tenant.Id}", new Response(tenant.Id, user.Id));
        })
        .WithName("Register")
        .WithTags("Auth")
        .AllowAnonymous();

        return app;
    }
}
