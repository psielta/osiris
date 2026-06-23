using Osiris.Application.Common.Interfaces;

namespace Osiris.Application.UnitTests.Features.AiAssistant.Support;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(Guid tenantId, string userId = "user-1")
    {
        TenantId = tenantId;
        UserId = userId;
    }

    public Guid TenantId { get; }

    public string? UserId { get; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
}
