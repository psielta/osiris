using MediatR;
using Osiris.Application.Features.Dashboard.DTOs;

namespace Osiris.Application.Features.Dashboard.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardVm>
{
    public Task<DashboardVm> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var metrics = new[]
        {
            new DashboardMetricDto("Users", "1", "Initial workspace owner"),
            new DashboardMetricDto("Customers", "0", "No customers yet"),
            new DashboardMetricDto("Revenue", "$0.00", "No revenue tracked"),
            new DashboardMetricDto("Activities", "0", "No recent activities")
        };

        return Task.FromResult(new DashboardVm(metrics));
    }
}
