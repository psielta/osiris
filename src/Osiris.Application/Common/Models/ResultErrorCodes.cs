namespace Osiris.Application.Common.Models;

public static class ResultErrorCodes
{
    public const string NotFound = "not_found";

    public const string Unauthorized = "unauthorized";

    public const string InvalidRefreshToken = "invalid_refresh_token";

    public const string LockedOut = "locked_out";

    public const string QuotaExceeded = "quota_exceeded";

    public const string Conflict = "conflict";
}
