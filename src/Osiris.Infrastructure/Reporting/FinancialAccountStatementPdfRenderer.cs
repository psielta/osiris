using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Application.Features.FinancialAccounts.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Osiris.Infrastructure.Reporting;

public sealed class FinancialAccountStatementPdfRenderer : IFinancialAccountStatementPdfRenderer
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public FinancialAccountStatementPdfRenderer(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public byte[] Render(FinancialAccountStatementDto statement)
    {
        // Chronological order so the running balance reads naturally and ends at CurrentBalance.
        var rows = new List<(MovementListItemDto Movement, decimal Balance)>(statement.Movements.Count);
        var balance = statement.InitialBalance;
        foreach (var movement in statement.Movements.OrderBy(movement => movement.OccurredOn))
        {
            balance += movement.IsInflow ? movement.Amount : -movement.Amount;
            rows.Add((movement, balance));
        }

        var generatedOn = $"Gerado em {PdfReportTheme.Date(PdfReportTheme.TodayInBrazil(_dateTimeProvider.UtcNow))}";

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
                    "Extrato da conta",
                    $"{statement.Name} · {PdfReportTheme.AccountType(statement.Type)}"));

                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Row(row =>
                    {
                        row.Spacing(8);
                        PdfReportTheme.SummaryCard(row, "Saldo inicial", PdfReportTheme.Brl(statement.InitialBalance), PdfReportTheme.Ink);
                        PdfReportTheme.SummaryCard(row, "Saldo atual", PdfReportTheme.Brl(statement.CurrentBalance), PdfReportTheme.Ink);
                        PdfReportTheme.SummaryCard(row, "Lançamentos", statement.Movements.Count.ToString(), PdfReportTheme.Ink);
                    });

                    if (rows.Count == 0)
                    {
                        column.Item().PaddingTop(24).AlignCenter()
                            .Text("Nenhum lançamento registrado nesta conta.")
                            .FontColor(PdfReportTheme.Muted);
                        return;
                    }

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(64);  // Data
                            columns.RelativeColumn(3);   // Descrição
                            columns.RelativeColumn(2);   // Tipo
                            columns.ConstantColumn(82);  // Valor
                            columns.ConstantColumn(86);  // Saldo
                        });

                        table.Header(header =>
                        {
                            PdfReportTheme.HeaderCell(header.Cell(), "Data");
                            PdfReportTheme.HeaderCell(header.Cell(), "Descrição");
                            PdfReportTheme.HeaderCell(header.Cell(), "Tipo");
                            PdfReportTheme.HeaderCell(header.Cell(), "Valor", alignRight: true);
                            PdfReportTheme.HeaderCell(header.Cell(), "Saldo", alignRight: true);
                        });

                        var index = 0;
                        foreach (var (movement, runningBalance) in rows)
                        {
                            var zebra = index++ % 2 == 1;
                            PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.Date(movement.OccurredOn));
                            PdfReportTheme.BodyCell(table.Cell(), zebra).Text(movement.Description);
                            PdfReportTheme.BodyCell(table.Cell(), zebra).Text(PdfReportTheme.MovementType(movement.Type));
                            PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight()
                                .Text($"{(movement.IsInflow ? "+" : "-")} {PdfReportTheme.Brl(movement.Amount)}")
                                .FontColor(movement.IsInflow ? PdfReportTheme.Inflow : PdfReportTheme.Outflow);
                            PdfReportTheme.BodyCell(table.Cell(), zebra).AlignRight()
                                .Text(PdfReportTheme.Brl(runningBalance));
                        }
                    });
                });

                page.Footer().Element(footer => PdfReportTheme.Footer(footer, generatedOn));
            });
        }).GeneratePdf();
    }
}
