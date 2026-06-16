using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Identity;

public sealed class RefreshTokenFactory : IRefreshTokenFactory
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly RefreshTokenOptions _options;

    public RefreshTokenFactory(IDateTimeProvider dateTimeProvider, IOptions<RefreshTokenOptions> options)
    {
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
    }

    public RefreshTokenCreation Create(string userId, Guid tenantId)
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = _dateTimeProvider.UtcNow.AddDays(_options.RefreshTokenDays);
        var token = new RefreshToken(Hash(rawToken), userId, tenantId, expiresAtUtc);
        return new RefreshTokenCreation(token, rawToken);
    }

    public string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
