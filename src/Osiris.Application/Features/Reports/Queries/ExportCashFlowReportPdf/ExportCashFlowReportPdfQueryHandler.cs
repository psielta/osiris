using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Dashboard.DTOs;
using Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;
using Osiris.Application.Features.Reports.DTOs;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Reports.Queries.ExportCashFlowReportPdf;

public sealed class ExportCashFlowReportPdfQueryHandler
    : IRequestHandler<ExportCashFlowReportPdfQuery, FileExportResult>
{
    private readonly ISender _sender;
    private readonly IBillRepository _bills;
    private readonly ICategoryRepository _categories;
    private readonly IFinancialAccountRepository _accounts;
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly ICreditCardRepository _creditCards;
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICreditCardStatementPaymentRepository _statementPayments;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICashFlowReportPdfRenderer _renderer;

    public ExportCashFlowReportPdfQueryHandler(
        ISender sender,
        IBillRepository bills,
        ICategoryRepository categories,
        IFinancialAccountRepository accounts,
        IFinancialAccountMovementRepository movements,
        ICreditCardRepository creditCards,
        ICreditCardStatementRepository statements,
        ICreditCardStatementPaymentRepository statementPayments,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ICashFlowReportPdfRenderer renderer)
    {
        _sender = sender;
        _bills = bills;
        _categories = categories;
        _accounts = accounts;
        _movements = movements;
        _creditCards = creditCards;
        _statements = statements;
        _statementPayments = statementPayments;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _renderer = renderer;
    }

    public async Task<FileExportResult> Handle(
        ExportCashFlowReportPdfQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await _sender.Send(
            new GetMonthlyDashboardSummaryQuery(request.Year, request.Month),
            cancellationToken);

        var report = request.Kind == CashFlowReportKind.Analytic
            ? await BuildAnalyticReportAsync(request, summary.CashFlow, cancellationToken)
            : new CashFlowReportDto(
                request.Kind,
                request.Year,
                request.Month,
                summary.CashFlow,
                Array.Empty<CashFlowReportAccountDto>(),
                Array.Empty<CashFlowReportMovementDto>(),
                Array.Empty<CashFlowReportBillDto>(),
                Array.Empty<CashFlowReportStatementPaymentDto>(),
                Array.Empty<CashFlowReportOpenStatementDto>());

        var content = _renderer.Render(report);
        var kindSlug = request.Kind == CashFlowReportKind.Analytic ? "analitica" : "sintetica";
        var fileName = $"visao-caixa-{kindSlug}-{request.Year:0000}-{request.Month:00}.pdf";
        return new FileExportResult(content, fileName);
    }

    private async Task<CashFlowReportDto> BuildAnalyticReportAsync(
        ExportCashFlowReportPdfQuery request,
        CashFlowSummaryDto cashFlow,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var categories = await _categories.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var allAccounts = await _accounts.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var monthMovements = await _movements.ListByMonthAsync(tenantId, request.Year, request.Month, cancellationToken);
        var monthBills = await _bills.ListByMonthAsync(tenantId, request.Year, request.Month, cancellationToken);
        var paidBillsInMonth = await _bills.ListPaidInMonthAsync(tenantId, request.Year, request.Month, cancellationToken);
        var payments = await _statementPayments.ListByMonthAsync(tenantId, request.Year, request.Month, cancellationToken);
        var statementsDueInMonth = await _statements.ListAsync(tenantId, monthStart, monthEnd, cancellationToken);
        var statementIds = statementsDueInMonth
            .Select(statement => statement.Id)
            .Concat(payments.Select(payment => payment.CreditCardStatementId))
            .Distinct()
            .ToArray();
        var paymentStatements = statementIds.Length == 0
            ? Array.Empty<CreditCardStatement>()
            : await _statements.ListByIdsAsync(tenantId, statementIds, cancellationToken);
        var allCards = await _creditCards.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var totalsById = await _statements.GetTotalsAsync(
            tenantId,
            statementsDueInMonth.Select(statement => statement.Id).ToArray(),
            cancellationToken);

        var categoryNames = categories.ToDictionary(category => category.Id, category => category.Name);
        var accountNames = allAccounts.ToDictionary(account => account.Id, account => account.Name);
        var cardNames = allCards.ToDictionary(card => card.Id, card => card.Name);
        var statementsById = paymentStatements.ToDictionary(statement => statement.Id);

        var accounts = allAccounts
            .Where(account => account.IsActive)
            .OrderBy(account => account.Name)
            .Select(account => new CashFlowReportAccountDto(
                account.Id,
                account.Name,
                account.Type,
                account.CurrentBalance))
            .ToArray();

        var movements = monthMovements
            .OrderBy(movement => movement.OccurredOn)
            .ThenBy(movement => movement.CreatedAtUtc)
            .Select(movement => new CashFlowReportMovementDto(
                movement.Id,
                movement.OccurredOn,
                accountNames.GetValueOrDefault(movement.FinancialAccountId, "Conta"),
                movement.Type,
                movement.Amount,
                movement.Type.IsInflow(),
                movement.Description,
                movement.CategoryId is null ? null : categoryNames.GetValueOrDefault(movement.CategoryId.Value),
                movement.Notes))
            .ToArray();

        var bills = monthBills
            .Concat(paidBillsInMonth)
            .DistinctBy(bill => bill.Id)
            .OrderBy(bill => bill.DueDate)
            .ThenBy(bill => bill.Description)
            .Select(bill => new CashFlowReportBillDto(
                bill.Id,
                bill.Description,
                bill.Amount,
                bill.DueDate,
                bill.PaidAt,
                Bill.CalculateStatus(bill.PaidAt, bill.DueDate, today),
                categoryNames.GetValueOrDefault(bill.CategoryId),
                bill.PaymentAccountId is null
                    ? null
                    : accountNames.GetValueOrDefault(bill.PaymentAccountId.Value)))
            .ToArray();

        var statementPayments = payments
            .OrderBy(payment => payment.PaidAt)
            .ThenBy(payment => payment.CreatedAtUtc)
            .Select(payment =>
            {
                statementsById.TryGetValue(payment.CreditCardStatementId, out var statement);
                var cardName = statement is null
                    ? "Cartao"
                    : cardNames.GetValueOrDefault(statement.CreditCardId, "Cartao");

                return new CashFlowReportStatementPaymentDto(
                    payment.Id,
                    payment.PaidAt,
                    cardName,
                    statement?.ReferenceMonth ?? 0,
                    statement?.ReferenceYear ?? 0,
                    payment.FinancialAccountId is null
                        ? null
                        : accountNames.GetValueOrDefault(payment.FinancialAccountId.Value),
                    payment.Amount,
                    payment.Notes);
            })
            .ToArray();

        var openStatements = statementsDueInMonth
            .Select(statement =>
            {
                var totals = totalsById.GetValueOrDefault(statement.Id, new CreditCardStatementTotals(0m, 0m));
                var status = CreditCardStatement.CalculateStatus(
                    totals.InstallmentsTotal,
                    totals.PaymentsTotal,
                    statement.ClosingDate,
                    statement.DueDate,
                    today);

                return new
                {
                    Statement = statement,
                    Status = status,
                    Totals = totals
                };
            })
            .Where(entry => entry.Totals.OpenBalance > 0m)
            .OrderBy(entry => entry.Statement.DueDate)
            .ThenBy(entry => cardNames.GetValueOrDefault(entry.Statement.CreditCardId, "Cartao"))
            .Select(entry => new CashFlowReportOpenStatementDto(
                entry.Statement.Id,
                cardNames.GetValueOrDefault(entry.Statement.CreditCardId, "Cartao"),
                entry.Statement.ReferenceMonth,
                entry.Statement.ReferenceYear,
                entry.Statement.DueDate,
                entry.Status,
                entry.Totals.InstallmentsTotal,
                entry.Totals.PaymentsTotal,
                entry.Totals.OpenBalance))
            .ToArray();

        return new CashFlowReportDto(
            request.Kind,
            request.Year,
            request.Month,
            cashFlow,
            accounts,
            movements,
            bills,
            statementPayments,
            openStatements);
    }
}
