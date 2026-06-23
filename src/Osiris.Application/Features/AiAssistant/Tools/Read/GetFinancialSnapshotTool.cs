using System.Globalization;
using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.Dashboard.DTOs;
using Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>
/// Read-only tool that returns a compact monthly financial snapshot (balances, cash flow, spending by
/// category, cards and upcoming obligations) by delegating to the existing dashboard query. All numbers
/// are computed server-side and tenant-scoped via <c>ICurrentUser</c>; the model only formats them.
/// </summary>
public sealed class GetFinancialSnapshotTool : IAiTool
{
    private const int MaxListItems = 10;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ISender _sender;

    public GetFinancialSnapshotTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "get_financial_snapshot";

    public string Description =>
        "Retorna um panorama financeiro do mês de referência: total de receitas e despesas, fluxo de caixa, "
        + "saldo das contas, gastos por categoria, situação dos cartões e próximos vencimentos. "
        + "Use para perguntas amplas sobre a situação financeira do usuário.";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            referenceDate = new
            {
                type = "string",
                description = "Data de referência no formato ISO yyyy-MM-dd. Opcional; o padrão é a data de hoje."
            }
        }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var referenceDate = ResolveReferenceDate(arguments, context.Today);

        var summary = await _sender.Send(
            new GetMonthlyDashboardSummaryQuery(referenceDate.Year, referenceDate.Month),
            cancellationToken);

        var payload = BuildPayload(summary);
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        return AiToolResult.Success(json, BuildSources(summary));
    }

    private static DateOnly ResolveReferenceDate(JsonElement arguments, DateOnly today)
    {
        if (arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty("referenceDate", out var value)
            && value.ValueKind == JsonValueKind.String
            && DateOnly.TryParse(
                value.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed;
        }

        return today;
    }

    private static object BuildPayload(MonthlyDashboardSummaryDto summary)
    {
        return new
        {
            period = new { year = summary.Year, month = summary.Month },
            currency = "BRL",
            incomeTotal = summary.IncomeTotal,
            spendingTotal = summary.SpendingTotal,
            cashFlow = new
            {
                summary.CashFlow.IncomeTotal,
                summary.CashFlow.BillsPaidTotal,
                summary.CashFlow.StatementPaymentsTotal,
                summary.CashFlow.DirectExpensesTotal,
                summary.CashFlow.BillsOpenInMonthTotal,
                summary.CashFlow.StatementsOpenInMonthTotal,
                summary.CashFlow.TotalAccountsBalance,
                summary.CashFlow.ProjectedCashBalance
            },
            spendingByCategory = summary.SpendingByCategory
                .OrderByDescending(category => category.Total)
                .Take(MaxListItems)
                .Select(category => new { category = category.CategoryName, total = category.Total }),
            creditCards = summary.CreditCards
                .Take(MaxListItems)
                .Select(card => new
                {
                    card.Name,
                    card.Limit,
                    card.UsedLimit,
                    card.AvailableLimit,
                    card.UsagePercentage,
                    card.CurrentStatementOpenBalance,
                    card.CurrentStatementDueDate,
                    currentStatementStatus = card.CurrentStatementStatus?.ToString()
                }),
            upcomingObligations = summary.UpcomingObligations
                .OrderBy(obligation => obligation.DueDate)
                .Take(MaxListItems)
                .Select(obligation => new
                {
                    kind = obligation.Kind.ToString(),
                    obligation.Description,
                    obligation.DueDate,
                    obligation.Amount,
                    obligation.IsOverdue
                }),
            alerts = summary.Alerts.Select(alert => new
            {
                severity = alert.Severity.ToString(),
                alert.Message
            })
        };
    }

    private static IReadOnlyList<AiSource> BuildSources(MonthlyDashboardSummaryDto summary)
    {
        var sources = new List<AiSource>();

        foreach (var card in summary.CreditCards.Take(MaxListItems))
        {
            sources.Add(new AiSource("creditCard", card.CreditCardId.ToString(), card.Name));
        }

        foreach (var obligation in summary.UpcomingObligations.Take(MaxListItems))
        {
            var type = obligation.Kind == UpcomingObligationKind.Bill ? "bill" : "creditCardStatement";
            sources.Add(new AiSource(type, obligation.Id.ToString(), obligation.Description));
        }

        return sources;
    }
}
