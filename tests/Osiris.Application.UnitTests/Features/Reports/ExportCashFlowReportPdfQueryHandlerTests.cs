using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.Dashboard.DTOs;
using Osiris.Application.Features.Reports.DTOs;
using Osiris.Application.Features.Reports.Queries.ExportCashFlowReportPdf;
using Osiris.Application.UnitTests.Common;
using Osiris.Application.UnitTests.Features.Dashboard.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Reports;

public sealed class ExportCashFlowReportPdfQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeBillRepository _bills = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeFinancialAccountRepository _accounts = new();
    private readonly FakeFinancialAccountMovementRepository _movements = new();
    private readonly FakeCreditCardRepository _cards = new();
    private readonly FakeCreditCardPurchaseRepository _purchases = new();
    private readonly FakeCreditCardStatementPaymentRepository _statementPayments = new();
    private readonly FakeCreditCardStatementRepository _statements;
    private readonly FakeCashFlowReportPdfRenderer _renderer = new();

    public ExportCashFlowReportPdfQueryHandlerTests()
    {
        _statements = new FakeCreditCardStatementRepository(_statementPayments);
    }

    [Fact]
    public async Task Handle_Synthetic_ShouldRenderDashboardCashFlowOnly()
    {
        var cashFlow = CashFlow(projected: 500m);
        var handler = CreateHandler(cashFlow);

        var result = await handler.Handle(
            new ExportCashFlowReportPdfQuery(2026, 6, CashFlowReportKind.Synthetic),
            CancellationToken.None);

        Assert.Equal("visao-caixa-sintetica-2026-06.pdf", result.FileName);
        Assert.Same(_renderer.Content, result.Content);
        Assert.NotNull(_renderer.Received);
        Assert.Equal(CashFlowReportKind.Synthetic, _renderer.Received!.Kind);
        Assert.Equal(500m, _renderer.Received.CashFlow.ProjectedCashBalance);
        Assert.Empty(_renderer.Received.Accounts);
        Assert.Empty(_renderer.Received.Movements);
        Assert.Empty(_renderer.Received.Bills);
        Assert.Empty(_renderer.Received.StatementPayments);
        Assert.Empty(_renderer.Received.OpenStatements);
    }

    [Fact]
    public async Task Handle_Analytic_ShouldRenderCashFlowDetails()
    {
        var account = new FinancialAccount(_tenantId, "Banco", FinancialAccountType.CheckingAccount, 1000m);
        _accounts.Add(account);
        var category = new FinancialCategory(_tenantId, "Casa", CategoryType.Expense);
        _categories.Add(category);
        var bill = new Bill(_tenantId, category.Id, "Internet", 80m, new DateOnly(2026, 6, 15), account.Id);
        _bills.Add(bill);
        var paidBill = new Bill(_tenantId, category.Id, "Escola", 200m, new DateOnly(2026, 5, 20), account.Id);
        paidBill.MarkAsPaid(new DateOnly(2026, 6, 2), account.Id, DateTime.UtcNow);
        _bills.Add(paidBill);
        _movements.Add(new FinancialAccountMovement(
            _tenantId,
            account.Id,
            FinancialAccountMovementType.Income,
            1500m,
            new DateOnly(2026, 6, 1),
            "Salario"));

        var card = new CreditCard(_tenantId, "Nubank", 5000m, 25, 5, account.Id);
        _cards.Add(card);
        var statement = new CreditCardStatement(
            _tenantId,
            card.Id,
            5,
            2026,
            new DateOnly(2026, 5, 25),
            new DateOnly(2026, 6, 5));
        _statements.Add(statement, installmentsTotal: 300m);
        _statementPayments.Add(new CreditCardStatementPayment(
            _tenantId,
            statement.Id,
            account.Id,
            50m,
            new DateOnly(2026, 6, 3)));

        var handler = CreateHandler(CashFlow(projected: 670m));

        var result = await handler.Handle(
            new ExportCashFlowReportPdfQuery(2026, 6, CashFlowReportKind.Analytic),
            CancellationToken.None);

        Assert.Equal("visao-caixa-analitica-2026-06.pdf", result.FileName);
        var report = _renderer.Received!;
        Assert.Equal(CashFlowReportKind.Analytic, report.Kind);
        Assert.Equal(670m, report.CashFlow.ProjectedCashBalance);
        Assert.Single(report.Accounts, item => item.Name == "Banco" && item.CurrentBalance == 1000m);
        Assert.Single(report.Movements, item => item.Description == "Salario" && item.IsInflow);
        Assert.Contains(report.Bills, item => item.Description == "Internet" && item.Status == BillStatus.Pending);
        Assert.Contains(report.Bills, item => item.Description == "Escola" && item.Status == BillStatus.Paid);
        Assert.Single(report.StatementPayments, item => item.CreditCardName == "Nubank" && item.Amount == 50m);
        Assert.Single(report.OpenStatements, item => item.CreditCardName == "Nubank" && item.OpenBalance == 250m);
    }

    private ExportCashFlowReportPdfQueryHandler CreateHandler(CashFlowSummaryDto cashFlow)
    {
        return new ExportCashFlowReportPdfQueryHandler(
            new StubSender(Summary(cashFlow)),
            _bills,
            _categories,
            _accounts,
            _movements,
            _cards,
            _statements,
            _statementPayments,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc) },
            _renderer);
    }

    private static CashFlowSummaryDto CashFlow(decimal projected) =>
        new(
            IncomeTotal: 1500m,
            BillsPaidTotal: 200m,
            StatementPaymentsTotal: 50m,
            DirectExpensesTotal: 0m,
            BillsOpenInMonthTotal: 80m,
            StatementsOpenInMonthTotal: 250m,
            TotalAccountsBalance: 1000m,
            ProjectedCashBalance: projected);

    private static MonthlyDashboardSummaryDto Summary(CashFlowSummaryDto cashFlow) =>
        new(
            Year: 2026,
            Month: 6,
            Onboarding: new OnboardingDto(true, true, true, true),
            IncomeTotal: cashFlow.IncomeTotal,
            SpendingTotal: 0m,
            SpendingByCategory: Array.Empty<SpendingByCategoryDto>(),
            CashFlow: cashFlow,
            CreditCards: Array.Empty<CreditCardDashboardDto>(),
            UpcomingObligations: Array.Empty<UpcomingObligationDto>(),
            Alerts: Array.Empty<DashboardAlertDto>(),
            BillsDueInMonthTotal: 80m,
            BillsDueInMonthCount: 1,
            BillsOpenInMonthTotal: cashFlow.BillsOpenInMonthTotal,
            StatementsDueInMonthTotal: 300m,
            StatementsDueInMonthCount: 1,
            StatementsOpenInMonthTotal: cashFlow.StatementsOpenInMonthTotal,
            TotalOpenStatementsBalance: cashFlow.StatementsOpenInMonthTotal,
            TotalOpenBillsBalance: cashFlow.BillsOpenInMonthTotal,
            StatementPaymentsInMonthTotal: cashFlow.StatementPaymentsTotal,
            FutureInstallmentsTotal: 0m,
            OverdueStatementsCount: 0,
            OverdueStatementsBalance: 0m,
            PartiallyPaidStatementsCount: 1);
}

internal sealed class FakeCashFlowReportPdfRenderer : ICashFlowReportPdfRenderer
{
    public byte[] Content { get; } = { 0x25, 0x50, 0x44, 0x46 };

    public CashFlowReportDto? Received { get; private set; }

    public byte[] Render(CashFlowReportDto report)
    {
        Received = report;
        return Content;
    }
}
