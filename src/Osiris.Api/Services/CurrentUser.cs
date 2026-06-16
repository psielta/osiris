using System.Security.Claims;
using Osiris.Application.Common;
using Osiris.Application.Common.Interfaces;

namespace Osiris.Api.Services;

/// <summary>
/// Reads the current user from the validated JWT (mirror of the Web host's cookie-backed CurrentUser).
/// The access token carries the same claims, so tenant resolution is identical.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var value = User?.FindFirstValue(OsirisClaimTypes.TenantId);
            if (!Guid.TryParse(value, out var tenantId))
            {
                throw new InvalidOperationException("The current user tenant id is missing or invalid.");
            }

            return tenantId;
        }
    }

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}
