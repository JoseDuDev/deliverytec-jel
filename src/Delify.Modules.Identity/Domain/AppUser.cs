using Microsoft.AspNetCore.Identity;

namespace Delify.Modules.Identity.Domain;

public sealed class AppUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsSuperAdmin { get; set; } = false;
}
