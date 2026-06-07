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
            metric => Assert.Equal("Usuários", metric.Label),
            metric => Assert.Equal("Clientes", metric.Label),
            metric => Assert.Equal("Receita", metric.Label),
            metric => Assert.Equal("Atividades", metric.Label));
    }
}
