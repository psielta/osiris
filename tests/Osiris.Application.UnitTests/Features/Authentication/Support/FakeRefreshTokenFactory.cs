using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Authentication.Support;

internal sealed class FakeRefreshTokenFactory : IRefreshTokenFactory
{
    private readonly DateTime _expiresAtUtc;
    private int _counter;

    public FakeRefreshTokenFactory(DateTime expiresAtUtc)
    {
        _expiresAtUtc = expiresAtUtc;
    }

    public RefreshTokenCreation Create(string userId, Guid tenantId)
    {
        _counter++;
        var rawToken = $"raw-{_counter}";
        var token = new RefreshToken(Hash(rawToken), userId, tenantId, _expiresAtUtc);
        return new RefreshTokenCreation(token, rawToken);
    }

    public string Hash(string rawToken) => $"hash:{rawToken}";
}
