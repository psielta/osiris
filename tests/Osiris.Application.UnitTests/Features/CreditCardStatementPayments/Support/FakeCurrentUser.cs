using Osiris.Application.Common.Interfaces;

namespace Osiris.Application.UnitTests.Features.CreditCardStatementPayments.Support;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public Guid TenantId { get; }

    public string? UserId => "user-1";

    public bool IsAuthenticated => true;
}
