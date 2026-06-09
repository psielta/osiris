using MediatR;
using Osiris.Application.Features.Dashboard.DTOs;

namespace Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;

public sealed record GetMonthlyDashboardSummaryQuery(int Year, int Month)
    : IRequest<MonthlyDashboardSummaryDto>;
