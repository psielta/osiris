using Osiris.Application.Common.Interfaces;

namespace Osiris.Application.UnitTests.Features.Authentication.Support;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(string? userId, Guid tenantId)
    {
        UserId = userId;
        TenantId = tenantId;
    }

    public Guid TenantId { get; }

    public string? UserId { get; }

    public bool IsAuthenticated => UserId is not null;
}
