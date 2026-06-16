using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Models;

/// <summary>
/// The result of minting a refresh token: the persistable entity (carrying only the hash) plus the
/// raw token value, which is returned to the client exactly once and never stored.
/// </summary>
public sealed record RefreshTokenCreation(RefreshToken Token, string RawToken);
