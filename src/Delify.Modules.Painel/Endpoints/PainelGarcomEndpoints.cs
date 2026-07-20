using Delify.Modules.Identity.Domain;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Painel.Endpoints;

internal static class PainelGarcomEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/painel/garcons")
            .RequireAuthorization()
            .WithTags("Painel");

        // ── Listar garçons ──────────────────────────────────────────────────
        group.MapGet("/", async (ITenantContext tenant, UserManager<AppUser> users) =>
        {
            var list = await users.Users
                .Where(u => u.TenantId == tenant.TenantId && u.Role == UserRole.Waiter)
                .OrderBy(u => u.FullName)
                .Select(u => new GarcomResponse(u.Id, u.FullName, u.Email!, u.CreatedAt))
                .ToListAsync();

            return Results.Ok(list);
        });

        // ── Criar garçom ────────────────────────────────────────────────────
        group.MapPost("/", async (ITenantContext tenant, UserManager<AppUser> users, CreateGarcomReq req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Nome, e-mail e senha são obrigatórios." });

            if (await users.FindByEmailAsync(req.Email) is not null)
                return Results.Conflict(new { error = "E-mail já cadastrado." });

            var waiter = new AppUser
            {
                UserName = req.Email.Trim(),
                Email = req.Email.Trim(),
                EmailConfirmed = true,
                FullName = req.Name.Trim(),
                TenantId = tenant.TenantId,
                Role = UserRole.Waiter,
            };

            var result = await users.CreateAsync(waiter, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            return Results.Created($"/painel/garcons/{waiter.Id}",
                new GarcomResponse(waiter.Id, waiter.FullName, waiter.Email!, waiter.CreatedAt));
        });

        // ── Editar garçom (nome e/ou senha) ─────────────────────────────────
        group.MapPatch("/{id:guid}", async (
            Guid id, ITenantContext tenant, UserManager<AppUser> users, UpdateGarcomReq req) =>
        {
            var waiter = await users.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenant.TenantId && u.Role == UserRole.Waiter);
            if (waiter is null) return Results.NotFound();

            if (!string.IsNullOrWhiteSpace(req.Name))
            {
                waiter.FullName = req.Name.Trim();
                await users.UpdateAsync(waiter);
            }

            if (!string.IsNullOrWhiteSpace(req.Password))
            {
                var token = await users.GeneratePasswordResetTokenAsync(waiter);
                var reset = await users.ResetPasswordAsync(waiter, token, req.Password);
                if (!reset.Succeeded)
                    return Results.BadRequest(new { errors = reset.Errors.Select(e => e.Description) });
            }

            return Results.Ok(new GarcomResponse(waiter.Id, waiter.FullName, waiter.Email!, waiter.CreatedAt));
        });

        // ── Excluir garçom ──────────────────────────────────────────────────
        group.MapDelete("/{id:guid}", async (Guid id, ITenantContext tenant, UserManager<AppUser> users) =>
        {
            var waiter = await users.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenant.TenantId && u.Role == UserRole.Waiter);
            if (waiter is null) return Results.NotFound();

            await users.DeleteAsync(waiter);
            return Results.NoContent();
        });

        return app;
    }

    private record CreateGarcomReq(string Name, string Email, string Password);
    private record UpdateGarcomReq(string? Name, string? Password);
}

internal record GarcomResponse(Guid Id, string Name, string Email, DateTimeOffset CreatedAt);
