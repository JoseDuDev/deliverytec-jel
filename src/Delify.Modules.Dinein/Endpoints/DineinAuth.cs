using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Delify.Modules.Dinein.Endpoints;

internal static class DineinAuth
{
    // Valida um JWT (painel ou garçom) passado via query string (EventSource não
    // envia header Authorization) e devolve o tenant_id.
    public static Guid? ValidateTenant(string? token, IConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var jwtKey = config["Jwt:Key"];
        if (jwtKey is null) return null;

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var tenantIdClaim = principal.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(tenantIdClaim, out var id) ? id : null;
        }
        catch { return null; }
    }
}
