using MediatR;
using Osiris.Application.Features.Dashboard.DTOs;

namespace Osiris.Application.Features.Dashboard.Queries.GetDashboard;

public sealed record GetDashboardQuery : IRequest<DashboardVm>;
