using Osiris.Application.Common.Interfaces;

namespace Osiris.Application.UnitTests.Features.Authentication.Support;

internal sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public AccessToken Generate(string userId, Guid tenantId, string email)
    {
        return new AccessToken($"access-{userId}", new DateTime(2026, 6, 16, 12, 15, 0, DateTimeKind.Utc));
    }
}
