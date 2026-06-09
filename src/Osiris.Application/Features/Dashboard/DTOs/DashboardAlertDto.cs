namespace Osiris.Application.Features.Dashboard.DTOs;

public enum DashboardAlertSeverity
{
    Info = 1,
    Warning = 2,
    Danger = 3
}

public sealed record DashboardAlertDto(DashboardAlertSeverity Severity, string Message);
