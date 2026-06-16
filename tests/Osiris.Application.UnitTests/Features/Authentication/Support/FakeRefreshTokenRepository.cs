using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.Authentication.Support;

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly List<RefreshToken> _tokens = new();

    public IReadOnlyList<RefreshToken> Tokens => _tokens;

    public bool RotateShouldConflict { get; set; }

    public int RevokeAllCallCount { get; private set; }

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        _tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = _tokens.FirstOrDefault(stored => stored.TokenHash == tokenHash);
        return Task.FromResult(token);
    }

    public Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        if (!_tokens.Contains(token))
        {
            _tokens.Add(token);
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryRotateAsync(RefreshToken current, RefreshToken replacement, CancellationToken cancellationToken)
    {
        if (RotateShouldConflict)
        {
            return Task.FromResult(false);
        }

        if (!_tokens.Contains(current))
        {
            _tokens.Add(current);
        }

        _tokens.Add(replacement);
        return Task.FromResult(true);
    }

    public Task RevokeAllForUserAsync(string userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        RevokeAllCallCount++;

        foreach (var token in _tokens.Where(stored => stored.UserId == userId && stored.RevokedAtUtc == null))
        {
            token.Revoke(utcNow);
        }

        return Task.CompletedTask;
    }
}
