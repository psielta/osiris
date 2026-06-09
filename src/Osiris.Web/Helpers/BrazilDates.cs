namespace Osiris.Web.Helpers;

/// <summary>
/// The app serves Brazilian users; form date defaults come from Brasília time so they match the
/// user's local day rather than the server's UTC day around midnight.
/// </summary>
public static class BrazilDates
{
    private static readonly TimeZoneInfo BrazilTimeZone = ResolveBrazilTimeZone();

    public static DateOnly Today()
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTimeZone));
    }

    private static TimeZoneInfo ResolveBrazilTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
