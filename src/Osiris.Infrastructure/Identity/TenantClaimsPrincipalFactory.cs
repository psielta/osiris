using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Osiris.Application.Common;

namespace Osiris.Infrastructure.Identity;

public sealed class TenantClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public TenantClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(OsirisClaimTypes.TenantId, user.TenantId.ToString()));

        return identity;
    }
}
