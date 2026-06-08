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

namespace Delify.Modules.Identity.Endpoints;

internal static class LoginEndpoint
{
    internal record Request(string Email, string Password);
    internal record Response(string Token, DateTimeOffset ExpiresAt);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            Request req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Unauthorized();

            var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTimeOffset.UtcNow.AddHours(8);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim("full_name", user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: expires.UtcDateTime,
                signingCredentials: creds);

            return Results.Ok(new Response(new JwtSecurityTokenHandler().WriteToken(token), expires));
        })
        .WithName("Login")
        .WithTags("Auth")
        .AllowAnonymous();

        return app;
    }
}
