using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Delify.Modules.Identity.Infrastructure;

public sealed class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid TenantId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User
                .FindFirst("tenant_id")?.Value;

            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }
}
