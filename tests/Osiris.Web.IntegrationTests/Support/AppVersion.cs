using System.Reflection;
using Osiris.Web.Controllers;

namespace Osiris.Web.IntegrationTests.Support;

internal static class AppVersion
{
    /// <summary>
    /// The version label rendered in the authenticated sidebar, derived from the Web assembly so
    /// version bumps do not break tests.
    /// </summary>
    public static string SidebarLabel
    {
        get
        {
            var assembly = typeof(DashboardController).Assembly;
            var version = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                .Split('+')[0]
                ?? assembly.GetName().Version?.ToString(3)
                ?? "0.1.0";

            return $"Osiris v{version}";
        }
    }
}
