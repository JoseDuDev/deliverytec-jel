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

namespace Delify.Modules.Bff.Endpoints;

internal static class AuthEndpoints
{
    private record GuestRequest(string Name, string Phone);
    private record RegisterRequest(string Name, string Email, string Password, string Phone);
    private record LoginRequest(string Email, string Password);
    private record TokenResponse(string Token, DateTimeOffset ExpiresAt);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bff/auth").WithTags("BFF").AllowAnonymous();

        group.MapPost("/guest", (GuestRequest req, IConfiguration config) =>
        {
            var expires = DateTimeOffset.UtcNow.AddHours(24);
            var token = BuildToken(config, expires,
                new Claim("role", "guest"),
                new Claim("name", req.Name),
                new Claim("phone", req.Phone),
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()));

            return Results.Ok(new TokenResponse(token, expires));
        })
        .WithName("BffGuestSession");

        group.MapPost("/register", async (
            RegisterRequest req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            var user = new AppUser
            {
                UserName = req.Email,
                Email = req.Email,
                FullName = req.Name,
                TenantId = Guid.Empty
            };

            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors.Select(e => e.Description));

            var expires = DateTimeOffset.UtcNow.AddDays(7);
            var token = BuildToken(config, expires,
                new Claim("role", "customer"),
                new Claim("name", req.Name),
                new Claim("phone", req.Phone),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!));

            return Results.Ok(new TokenResponse(token, expires));
        })
        .WithName("BffRegister");

        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null || user.TenantId != Guid.Empty)
                return Results.Unauthorized();

            if (!await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Unauthorized();

            var expires = DateTimeOffset.UtcNow.AddDays(7);
            var token = BuildToken(config, expires,
                new Claim("role", "customer"),
                new Claim("name", user.FullName),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!));

            return Results.Ok(new TokenResponse(token, expires));
        })
        .WithName("BffLogin");

        return app;
    }

    private static string BuildToken(IConfiguration config, DateTimeOffset expires, params Claim[] claims)
    {
        var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
