namespace Osiris.Api.Controllers.V1;

internal static class ApiDateDefaults
{
    public static DateTime TodayInSaoPaulo()
    {
        var timeZone = FindSaoPauloTimeZone();
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
    }

    private static TimeZoneInfo FindSaoPauloTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}
