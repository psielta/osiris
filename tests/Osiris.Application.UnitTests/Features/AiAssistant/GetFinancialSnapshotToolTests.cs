using System.Text.Json;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Tools.Read;
using Osiris.Application.Features.Dashboard.DTOs;
using Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class GetFinancialSnapshotToolTests
{
    private static readonly Guid CardId = Guid.NewGuid();
    private static readonly Guid BillId = Guid.NewGuid();

    private static AiAgentContext Context() =>
        new(Guid.NewGuid(), "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 22), false);

    private static MonthlyDashboardSummaryDto BuildSummary() => new(
        Year: 2026,
        Month: 6,
        Onboarding: new OnboardingDto(true, true, true, true),
        IncomeTotal: 5000m,
        SpendingTotal: 3200m,
        SpendingByCategory: new[]
        {
            new SpendingByCategoryDto(Guid.NewGuid(), "Alimentação", "#FFFFFF", 1000m, 200m, 50m)
        },
        CashFlow: new CashFlowSummaryDto(5000m, 800m, 300m, 1000m, 200m, 400m, 12000m, 11000m),
        CreditCards: new[]
        {
            new CreditCardDashboardDto(
                CardId, "Nubank", 5000m, 1200m, 3800m, 24m,
                Guid.NewGuid(), 1200m, 0m, 1200m, new DateOnly(2026, 7, 10),
                CreditCardStatementStatus.Open, 600m)
        },
        UpcomingObligations: new[]
        {
            new UpcomingObligationDto(UpcomingObligationKind.Bill, BillId, null, "Aluguel", new DateOnly(2026, 6, 25), 1200m, false)
        },
        Alerts: new[]
        {
            new DashboardAlertDto(DashboardAlertSeverity.Warning, "Fatura próxima do vencimento")
        },
        BillsDueInMonthTotal: 1200m,
        BillsDueInMonthCount: 1,
        BillsOpenInMonthTotal: 1200m,
        StatementsDueInMonthTotal: 1200m,
        StatementsDueInMonthCount: 1,
        StatementsOpenInMonthTotal: 1200m,
        TotalOpenStatementsBalance: 1200m,
        TotalOpenBillsBalance: 1200m,
        StatementPaymentsInMonthTotal: 300m,
        FutureInstallmentsTotal: 600m,
        OverdueStatementsCount: 0,
        OverdueStatementsBalance: 0m,
        PartiallyPaidStatementsCount: 0);

    [Fact]
    public async Task ExecuteAsync_ReturnsCompactSnapshot_WithSources()
    {
        var tool = new GetFinancialSnapshotTool(new FakeSender(BuildSummary()));

        var result = await tool.ExecuteAsync(
            JsonDocument.Parse("{}").RootElement,
            Context(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        using var document = JsonDocument.Parse(result.ResultJson);
        var root = document.RootElement;
        Assert.Equal("BRL", root.GetProperty("currency").GetString());
        Assert.Equal(5000m, root.GetProperty("incomeTotal").GetDecimal());
        Assert.Equal(2026, root.GetProperty("period").GetProperty("year").GetInt32());

        Assert.NotNull(result.Sources);
        Assert.Contains(result.Sources!, source => source.Type == "creditCard" && source.Label == "Nubank");
        Assert.Contains(result.Sources!, source => source.Type == "bill" && source.Label == "Aluguel");
    }

    [Fact]
    public async Task ExecuteAsync_UsesReferenceDateArgument_ToPickTheMonth()
    {
        var sender = new FakeSender(BuildSummary());
        var tool = new GetFinancialSnapshotTool(sender);

        await tool.ExecuteAsync(
            JsonDocument.Parse("{\"referenceDate\":\"2026-03-15\"}").RootElement,
            Context(),
            CancellationToken.None);

        var query = Assert.IsType<GetMonthlyDashboardSummaryQuery>(sender.LastRequest);
        Assert.Equal(2026, query.Year);
        Assert.Equal(3, query.Month);
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackToToday_WhenNoReferenceDateGiven()
    {
        var sender = new FakeSender(BuildSummary());
        var tool = new GetFinancialSnapshotTool(sender);

        await tool.ExecuteAsync(JsonDocument.Parse("{}").RootElement, Context(), CancellationToken.None);

        var query = Assert.IsType<GetMonthlyDashboardSummaryQuery>(sender.LastRequest);
        Assert.Equal(2026, query.Year);
        Assert.Equal(6, query.Month);
    }
}
