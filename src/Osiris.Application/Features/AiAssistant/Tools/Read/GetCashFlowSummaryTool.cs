using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Returns the cash view for a month: money in/out of accounts and projected balance.</summary>
public sealed class GetCashFlowSummaryTool : IAiTool
{
    private readonly ISender _sender;

    public GetCashFlowSummaryTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "get_cash_flow_summary";

    public string Description =>
        "Fluxo de caixa de um mês: entradas, saídas (incluindo pagamentos de fatura), saldo das contas e "
        + "saldo projetado. Visão diferente do resumo de despesas por categoria.";

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
        var cashFlow = summary.CashFlow;

        var payload = new
        {
            period = new { year, month },
            cashFlow.IncomeTotal,
            cashFlow.BillsPaidTotal,
            cashFlow.StatementPaymentsTotal,
            cashFlow.DirectExpensesTotal,
            cashFlow.BillsOpenInMonthTotal,
            cashFlow.StatementsOpenInMonthTotal,
            cashFlow.TotalAccountsBalance,
            cashFlow.ProjectedCashBalance
        };

        return AiToolResult.Success(AiToolJson.Serialize(payload));
    }
}
