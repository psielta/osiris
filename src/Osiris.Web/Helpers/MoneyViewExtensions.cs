using System.Globalization;

namespace Osiris.Web.Helpers;

public static class MoneyViewExtensions
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string ToBrl(this decimal value) => value.ToString("C", PtBr);
}
