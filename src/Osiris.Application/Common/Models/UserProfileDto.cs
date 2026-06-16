namespace Osiris.Application.Common.Models;

public sealed record UserProfileDto(
    string UserId,
    string Email,
    string FullName,
    Guid TenantId,
    string TenantName);
