using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Delify.Modules.Identity.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Delify.Modules.Painel.Endpoints;

internal static class PainelAuthEndpoints
{
    internal record LoginRequest(string Email, string Password);
    internal record LoginResponse(string Token, DateTimeOffset ExpiresAt);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/painel/auth/login", async (
            LoginRequest req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Unauthorized();

            if (user.IsSuperAdmin || user.TenantId == Guid.Empty)
                return Results.Forbid();

            var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTimeOffset.UtcNow.AddHours(8);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim("full_name", user.FullName),
            };

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: expires.UtcDateTime,
                signingCredentials: creds);

            return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires));
        })
        .WithTags("Painel")
        .AllowAnonymous();

        return app;
    }
}
