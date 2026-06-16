using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Osiris.Infrastructure.Reporting;

public sealed class CreditCardStatementPdfRenderer : ICreditCardStatementPdfRenderer
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreditCardStatementPdfRenderer(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public byte[] Render(CreditCardStatementDetailsDto statement)
    {
        var generatedOn = $"Gerado em {PdfReportTheme.Date(PdfReportTheme.TodayInBrazil(_dateTimeProvider.UtcNow))}";
        var reference = PdfReportTheme.MonthReference(statement.ReferenceMonth, statement.ReferenceYear);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor("#FFFFFF");
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(PdfReportTheme.Ink));

                page.Header().Element(header => PdfReportTheme.Header(
                    header,
                    $"Fatura · {reference}",
                    $"{statement.CreditCardName} · {PdfReportTheme.StatementStatus(statement.Status)}"));

                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Row(row =>
                    {
                        row.Spacing(8);
                        PdfReportTheme.SummaryCard(row, "Total da fatura", PdfReportTheme.Brl(statement.TotalAmount), PdfReportTheme.Ink);
                        PdfReportTheme.SummaryCard(row, "Total pago", PdfReportTheme.Brl(statement.PaidAmount), PdfReportTheme.Inflow);
                        PdfReportTheme.SummaryCard(
                            row,
                            "Saldo em aberto",
                            PdfReportTheme.Brl(statement.OpenBalance),
                            statement.OpenBalance > 0m ? PdfReportTheme.Outflow : PdfReportTheme.Ink);
                    });

                    column.Item().Row(row =>
                    {
                        row.Spacing(8);
                        PdfReportTheme.SummaryCard(row, "Fechamento", PdfReportTheme.Date(statement.ClosingDate), PdfReportTheme.Ink);
                        PdfReportTheme.SummaryCard(row, "Vencimento", PdfReportTheme.Date(statement.DueDate), PdfReportTheme.Ink);
                    });

                    column.Item().PaddingTop(2).Text("Parcelas da fatura").FontSize(12).Bold().FontColor(PdfReportTheme.Ink);
                    if (statement.InstallmentItems.Count == 0)
                    {
                        column.Item().Text("Nenhuma parcela nesta fatura.").FontColor(PdfReportTheme.Muted);
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);   // Compra
                                columns.RelativeColumn(1);   // Parcela
                                columns.ConstantColumn(92);  // Valor
                            });

                            table.Header(header =>
                            {
                                PdfReportTheme.HeaderCell(header.Cell(), "Compra");
                                PdfReportTheme.HeaderCell(header.Cell(), "Parcela");
                                PdfReportTheme.HeaderCell(header.Cell(), "Valor", alignRight: true);
                            });

                            var index = 0;
                            foreach (var installment in statement.InstallmentItems)
                            {
                                var zebra = index++ % 2 == 1;
                                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(installment.PurchaseDescription);
                                PdfReportTheme.BodyCell(table.Cell(), zebra)
                                    .Text($"{installment.InstallmentNumber} de {installment.TotalInstallments}");
                                PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight()
                                    .Text(PdfReportTheme.Brl(installment.Amount));
                            }
                        });
                    }

                    column.Item().PaddingTop(2).Text("Pagamentos").FontSize(12).Bold().FontColor(PdfReportTheme.Ink);
                    if (statement.Payments.Count == 0)
                    {
                        column.Item().Text("Nenhum pagamento registrado para esta fatura.").FontColor(PdfReportTheme.Muted);
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);  // Data
                                columns.RelativeColumn(1);   // Conta
                                columns.ConstantColumn(92);  // Valor
                            });

                            table.Header(header =>
                            {
                                PdfReportTheme.HeaderCell(header.Cell(), "Data");
                                PdfReportTheme.HeaderCell(header.Cell(), "Conta");
                                PdfReportTheme.HeaderCell(header.Cell(), "Valor", alignRight: true);
                            });

                            var index = 0;
                            foreach (var payment in statement.Payments)
                            {
                                var zebra = index++ % 2 == 1;
                                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.Date(payment.PaidAt));
                                PdfReportTheme.BodyCell(table.Cell(), zebra).Text(payment.FinancialAccountName ?? "Sem conta");
                                PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight()
                                    .Text(PdfReportTheme.Brl(payment.Amount)).FontColor(PdfReportTheme.Inflow);
                            }
                        });
                    }
                });

                page.Footer().Element(footer => PdfReportTheme.Footer(footer, generatedOn));
            });
        }).GeneratePdf();
    }
}
