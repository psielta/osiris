using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.Reports.DTOs;
using Osiris.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Osiris.Infrastructure.Reporting;

public sealed class CashFlowReportPdfRenderer : ICashFlowReportPdfRenderer
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public CashFlowReportPdfRenderer(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public byte[] Render(CashFlowReportDto report)
    {
        var generatedOn = $"Gerado em {PdfReportTheme.Date(PdfReportTheme.TodayInBrazil(_dateTimeProvider.UtcNow))}";
        var reference = PdfReportTheme.MonthReference(report.Month, report.Year);
        var isAnalytic = report.Kind == CashFlowReportKind.Analytic;
        var title = isAnalytic ? "Visão de caixa analítica" : "Visão de caixa sintética";

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor("#FFFFFF");
                page.DefaultTextStyle(style => style.FontSize(9).FontColor(PdfReportTheme.Ink));

                page.Header().Element(header => PdfReportTheme.Header(header, title, reference));

                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Spacing(14);
                    AddSummary(column, report);

                    if (!isAnalytic)
                    {
                        return;
                    }

                    AddAccounts(column, report);
                    AddMovements(column, report);
                    AddBills(column, report);
                    AddStatementPayments(column, report);
                    AddOpenStatements(column, report);
                });

                page.Footer().Element(footer => PdfReportTheme.Footer(footer, generatedOn));
            });
        }).GeneratePdf();
    }

    private static void AddSummary(ColumnDescriptor column, CashFlowReportDto report)
    {
        column.Item().Row(row =>
        {
            row.Spacing(8);
            PdfReportTheme.SummaryCard(row, "Receitas do mês", PdfReportTheme.Brl(report.CashFlow.IncomeTotal), PdfReportTheme.Inflow);
            PdfReportTheme.SummaryCard(row, "Saldo em contas", PdfReportTheme.Brl(report.CashFlow.TotalAccountsBalance), PdfReportTheme.Ink);
            PdfReportTheme.SummaryCard(row, "Saldo previsto", PdfReportTheme.Brl(report.CashFlow.ProjectedCashBalance), BalanceColor(report.CashFlow.ProjectedCashBalance));
        });

        column.Item().Row(row =>
        {
            row.Spacing(8);
            PdfReportTheme.SummaryCard(row, "Contas pagas", PdfReportTheme.Brl(report.CashFlow.BillsPaidTotal), PdfReportTheme.Outflow);
            PdfReportTheme.SummaryCard(row, "Pagamentos de fatura", PdfReportTheme.Brl(report.CashFlow.StatementPaymentsTotal), PdfReportTheme.Outflow);
            PdfReportTheme.SummaryCard(row, "Despesas diretas", PdfReportTheme.Brl(report.CashFlow.DirectExpensesTotal), PdfReportTheme.Outflow);
        });

        column.Item().Row(row =>
        {
            row.Spacing(8);
            PdfReportTheme.SummaryCard(row, "Contas em aberto", PdfReportTheme.Brl(report.CashFlow.BillsOpenInMonthTotal), PdfReportTheme.Outflow);
            PdfReportTheme.SummaryCard(row, "Faturas em aberto", PdfReportTheme.Brl(report.CashFlow.StatementsOpenInMonthTotal), PdfReportTheme.Outflow);
        });

        column.Item()
            .Border(1)
            .BorderColor(PdfReportTheme.Line)
            .Background(PdfReportTheme.HeaderRow)
            .Padding(10)
            .Text(text =>
            {
                text.Span("Saldo previsto após pagar tudo = ").Bold();
                text.Span($"{PdfReportTheme.Brl(report.CashFlow.TotalAccountsBalance)} - ");
                text.Span($"{PdfReportTheme.Brl(report.CashFlow.BillsOpenInMonthTotal)} - ");
                text.Span($"{PdfReportTheme.Brl(report.CashFlow.StatementsOpenInMonthTotal)} = ");
                text.Span(PdfReportTheme.Brl(report.CashFlow.ProjectedCashBalance)).Bold().FontColor(BalanceColor(report.CashFlow.ProjectedCashBalance));
            });
    }

    private static void AddAccounts(ColumnDescriptor column, CashFlowReportDto report)
    {
        AddSectionTitle(column, "Saldos por conta");
        if (report.Accounts.Count == 0)
        {
            AddEmpty(column, "Nenhuma conta financeira ativa.");
            return;
        }

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.ConstantColumn(100);
            });

            table.Header(header =>
            {
                PdfReportTheme.HeaderCell(header.Cell(), "Conta");
                PdfReportTheme.HeaderCell(header.Cell(), "Tipo");
                PdfReportTheme.HeaderCell(header.Cell(), "Saldo", alignRight: true);
            });

            var index = 0;
            foreach (var account in report.Accounts)
            {
                var zebra = index++ % 2 == 1;
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(account.Name);
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.AccountType(account.Type));
                PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight().Text(PdfReportTheme.Brl(account.CurrentBalance));
            }
        });
    }

    private static void AddMovements(ColumnDescriptor column, CashFlowReportDto report)
    {
        AddSectionTitle(column, "Movimentações do mês");
        if (report.Movements.Count == 0)
        {
            AddEmpty(column, "Nenhuma movimentação financeira no mês.");
            return;
        }

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(62);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.ConstantColumn(88);
            });

            table.Header(header =>
            {
                PdfReportTheme.HeaderCell(header.Cell(), "Data");
                PdfReportTheme.HeaderCell(header.Cell(), "Conta");
                PdfReportTheme.HeaderCell(header.Cell(), "Tipo");
                PdfReportTheme.HeaderCell(header.Cell(), "Descrição");
                PdfReportTheme.HeaderCell(header.Cell(), "Categoria");
                PdfReportTheme.HeaderCell(header.Cell(), "Valor", alignRight: true);
            });

            var index = 0;
            foreach (var movement in report.Movements)
            {
                var zebra = index++ % 2 == 1;
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.Date(movement.OccurredOn));
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(movement.FinancialAccountName);
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.MovementType(movement.Type));
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(movement.Description);
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(movement.CategoryName ?? "Sem categoria");
                PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight()
                    .Text($"{(movement.IsInflow ? "+" : "-")} {PdfReportTheme.Brl(movement.Amount)}")
                    .FontColor(movement.IsInflow ? PdfReportTheme.Inflow : PdfReportTheme.Outflow);
            }
        });
    }

    private static void AddBills(ColumnDescriptor column, CashFlowReportDto report)
    {
        AddSectionTitle(column, "Contas a pagar");
        if (report.Bills.Count == 0)
        {
            AddEmpty(column, "Nenhuma conta a pagar no período.");
            return;
        }

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.ConstantColumn(62);
                columns.ConstantColumn(62);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(86);
            });

            table.Header(header =>
            {
                PdfReportTheme.HeaderCell(header.Cell(), "Descrição");
                PdfReportTheme.HeaderCell(header.Cell(), "Venc.");
                PdfReportTheme.HeaderCell(header.Cell(), "Pago em");
                PdfReportTheme.HeaderCell(header.Cell(), "Situação");
                PdfReportTheme.HeaderCell(header.Cell(), "Conta");
                PdfReportTheme.HeaderCell(header.Cell(), "Valor", alignRight: true);
            });

            var index = 0;
            foreach (var bill in report.Bills)
            {
                var zebra = index++ % 2 == 1;
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(bill.Description);
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.Date(bill.DueDate));
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(bill.PaidAt is null ? "-" : PdfReportTheme.Date(bill.PaidAt.Value));
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(BillStatusLabel(bill.Status));
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(bill.PaymentAccountName ?? "Sem conta");
                PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight().Text(PdfReportTheme.Brl(bill.Amount));
            }
        });
    }

    private static void AddStatementPayments(ColumnDescriptor column, CashFlowReportDto report)
    {
        AddSectionTitle(column, "Pagamentos de fatura");
        if (report.StatementPayments.Count == 0)
        {
            AddEmpty(column, "Nenhum pagamento de fatura no mês.");
            return;
        }

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(62);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(86);
            });

            table.Header(header =>
            {
                PdfReportTheme.HeaderCell(header.Cell(), "Data");
                PdfReportTheme.HeaderCell(header.Cell(), "Cartão");
                PdfReportTheme.HeaderCell(header.Cell(), "Fatura");
                PdfReportTheme.HeaderCell(header.Cell(), "Conta");
                PdfReportTheme.HeaderCell(header.Cell(), "Valor", alignRight: true);
            });

            var index = 0;
            foreach (var payment in report.StatementPayments)
            {
                var zebra = index++ % 2 == 1;
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.Date(payment.PaidAt));
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(payment.CreditCardName);
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(StatementReference(payment.ReferenceMonth, payment.ReferenceYear));
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(payment.FinancialAccountName ?? "Sem conta");
                PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight().Text(PdfReportTheme.Brl(payment.Amount));
            }
        });
    }

    private static void AddOpenStatements(ColumnDescriptor column, CashFlowReportDto report)
    {
        AddSectionTitle(column, "Faturas em aberto com vencimento no mês");
        if (report.OpenStatements.Count == 0)
        {
            AddEmpty(column, "Nenhuma fatura em aberto com vencimento no mês.");
            return;
        }

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(62);
                columns.RelativeColumn(2);
                columns.ConstantColumn(78);
                columns.ConstantColumn(78);
                columns.ConstantColumn(86);
            });

            table.Header(header =>
            {
                PdfReportTheme.HeaderCell(header.Cell(), "Cartão");
                PdfReportTheme.HeaderCell(header.Cell(), "Fatura");
                PdfReportTheme.HeaderCell(header.Cell(), "Venc.");
                PdfReportTheme.HeaderCell(header.Cell(), "Situação");
                PdfReportTheme.HeaderCell(header.Cell(), "Total", alignRight: true);
                PdfReportTheme.HeaderCell(header.Cell(), "Pago", alignRight: true);
                PdfReportTheme.HeaderCell(header.Cell(), "Aberto", alignRight: true);
            });

            var index = 0;
            foreach (var statement in report.OpenStatements)
            {
                var zebra = index++ % 2 == 1;
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(statement.CreditCardName);
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(StatementReference(statement.ReferenceMonth, statement.ReferenceYear));
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.Date(statement.DueDate));
                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.StatementStatus(statement.Status));
                PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight().Text(PdfReportTheme.Brl(statement.TotalAmount));
                PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight().Text(PdfReportTheme.Brl(statement.PaidAmount));
                PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight().Text(PdfReportTheme.Brl(statement.OpenBalance));
            }
        });
    }

    private static void AddSectionTitle(ColumnDescriptor column, string title)
    {
        column.Item().PaddingTop(4).Text(title).FontSize(12).Bold().FontColor(PdfReportTheme.Ink);
    }

    private static void AddEmpty(ColumnDescriptor column, string message)
    {
        column.Item().Text(message).FontColor(PdfReportTheme.Muted);
    }

    private static string BalanceColor(decimal balance) => balance < 0m ? PdfReportTheme.Outflow : PdfReportTheme.Inflow;

    private static string StatementReference(int month, int year) =>
        month is >= 1 and <= 12 && year > 0 ? $"{month:00}/{year:0000}" : "Fatura";

    private static string BillStatusLabel(BillStatus status) => status switch
    {
        BillStatus.Pending => "Pendente",
        BillStatus.Paid => "Paga",
        BillStatus.Overdue => "Vencida",
        _ => status.ToString()
    };
}
