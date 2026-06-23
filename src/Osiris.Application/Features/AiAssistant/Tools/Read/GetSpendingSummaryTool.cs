using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Returns spending grouped by category for a month (purchases + bills + direct expenses).</summary>
public sealed class GetSpendingSummaryTool : IAiTool
{
    private readonly ISender _sender;

    public GetSpendingSummaryTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "get_spending_summary";

    public string Description =>
        "Resumo de despesas por categoria em um mês. Não confunda com fluxo de caixa: aqui contam compras "
        + "no cartão (na data da compra), contas a pagar e despesas diretas; pagamentos de fatura não entram.";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            referenceDate = new { type = "string", description = "Data de referência ISO yyyy-MM-dd (define o mês). Opcional; padrão hoje." }
        }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var (year, month) = AiToolArguments.ResolveMonth(arguments, context.Today);

        var summary = await _sender.Send(new GetMonthlyDashboardSummaryQuery(year, month), cancellationToken);

        var payload = new
        {
            period = new { year, month },
            spendingTotal = summary.SpendingTotal,
            byCategory = summary.SpendingByCategory
                .OrderByDescending(category => category.Total)
                .Select(category => new
                {
                    category.CategoryName,
                    total = category.Total,
                    category.CardPurchasesTotal,
                    category.BillsTotal,
                    category.DirectExpensesTotal
                })
        };

        return AiToolResult.Success(AiToolJson.Serialize(payload));
    }
}
