namespace Osiris.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid TenantId { get; }

    string? UserId { get; }

    bool IsAuthenticated { get; }
}
