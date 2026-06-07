namespace Osiris.Application.Features.Dashboard.DTOs;

public sealed record DashboardVm(IReadOnlyCollection<DashboardMetricDto> Metrics);

public sealed record DashboardMetricDto(string Label, string Value, string Description);
