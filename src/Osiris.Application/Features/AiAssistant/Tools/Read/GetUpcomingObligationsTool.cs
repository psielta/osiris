using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>
/// Returns bills and card statements coming due within a day window. Built from the monthly dashboard,
/// so it covers the reference month; for windows that cross a month boundary it may miss the next month.
/// </summary>
public sealed class GetUpcomingObligationsTool : IAiTool
{
    private const int DefaultWindowDays = 7;

    private readonly ISender _sender;

    public GetUpcomingObligationsTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "get_upcoming_obligations";

    public string Description =>
        "Lista as obrigações que vencem nos próximos dias (contas a pagar e faturas de cartão), "
        + "ordenadas por vencimento. Informe windowDays (padrão 7).";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            windowDays = new { type = "integer", description = "Janela em dias a partir de hoje. Padrão: 7." }
        }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var windowDays = AiToolArguments.GetInt(arguments, "windowDays") ?? DefaultWindowDays;
        if (windowDays < 1)
        {
            windowDays = DefaultWindowDays;
        }

        var limit = context.Today.AddDays(windowDays);

        var summary = await _sender.Send(
            new GetMonthlyDashboardSummaryQuery(context.Today.Year, context.Today.Month),
            cancellationToken);

        var upcoming = summary.UpcomingObligations
            .Where(obligation => obligation.DueDate <= limit)
            .OrderBy(obligation => obligation.DueDate)
            .Select(obligation => new
            {
                kind = obligation.Kind.ToString(),
                obligation.Description,
                obligation.DueDate,
                obligation.Amount,
                obligation.IsOverdue
            })
            .ToList();

        var payload = new
        {
            from = context.Today,
            to = limit,
            obligations = upcoming,
            obligationCount = upcoming.Count
        };

        return AiToolResult.Success(AiToolJson.Serialize(payload));
    }
}
