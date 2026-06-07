using Microsoft.AspNetCore.Identity;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; set; }
}
