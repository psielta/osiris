using Osiris.Application.Features.Dashboard.Queries.GetDashboard;

namespace Osiris.Application.UnitTests.Features.Dashboard;

public sealed class GetDashboardQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnInitialDashboardMetrics()
    {
        var handler = new GetDashboardQueryHandler();

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.Collection(
            result.Metrics,
            metric => Assert.Equal("Users", metric.Label),
            metric => Assert.Equal("Customers", metric.Label),
            metric => Assert.Equal("Revenue", metric.Label),
            metric => Assert.Equal("Activities", metric.Label));
    }
}
