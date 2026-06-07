using MediatR;
using Osiris.Application.Features.Dashboard.DTOs;

namespace Osiris.Application.Features.Dashboard.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardVm>
{
    public Task<DashboardVm> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var metrics = new[]
        {
            new DashboardMetricDto("Usuários", "1", "Dono inicial da área de trabalho"),
            new DashboardMetricDto("Clientes", "0", "Nenhum cliente cadastrado"),
            new DashboardMetricDto("Receita", "R$ 0,00", "Nenhuma receita acompanhada"),
            new DashboardMetricDto("Atividades", "0", "Nenhuma atividade recente")
        };

        return Task.FromResult(new DashboardVm(metrics));
    }
}
