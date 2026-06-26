using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Delify.Modules.Identity.Domain;
using Delify.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Delify.Modules.Admin.Endpoints;

internal static class AdminAuthEndpoints
{
    private record SetupRequest(string Email, string Password, string FullName);
    private record LoginRequest(string Email, string Password);
    private record TokenResponse(string Token, DateTimeOffset ExpiresAt);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/auth").WithTags("Admin").AllowAnonymous();

        group.MapPost("/setup", async (
            SetupRequest req,
            IdentityDbContext db,
            UserManager<AppUser> userManager) =>
        {
            if (await userManager.Users.AnyAsync(u => u.IsSuperAdmin))
                return Results.Conflict(new { error = "Super admin already exists." });

            var user = new AppUser
            {
                UserName = req.Email,
                Email = req.Email,
                FullName = req.FullName,
                TenantId = Guid.Empty,
                IsSuperAdmin = true
            };

            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors.Select(e => e.Description));

            return Results.Created("/admin/auth/login", new { message = "Super admin created." });
        })
        .WithName("AdminSetup");

        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null || !user.IsSuperAdmin)
                return Results.Unauthorized();

            if (!await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Unauthorized();

            var expires = DateTimeOffset.UtcNow.AddHours(8);
            var token = BuildToken(config, expires,
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("full_name", user.FullName),
                new Claim("is_super_admin", "true"));

            return Results.Ok(new TokenResponse(token, expires));
        })
        .WithName("AdminLogin");

        return app;
    }

    private static string BuildToken(IConfiguration config, DateTimeOffset expires, params Claim[] claims)
    {
        var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
