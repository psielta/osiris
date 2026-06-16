using System.Globalization;
using Osiris.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Osiris.Infrastructure.Reporting;

/// <summary>
/// Shared look-and-feel and pt-BR formatting for the generated PDF reports. The enum labels are
/// intentionally mirrored from the Web view helpers (which the Infrastructure layer cannot
/// reference); keep the strings in sync with <c>Osiris.Web.Helpers</c>.
/// </summary>
internal static class PdfReportTheme
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    // Palette aligned with the app's Tailwind colors.
    public const string Accent = "#B45309";   // amber-700
    public const string Ink = "#0F172A";      // slate-900
    public const string Muted = "#64748B";    // slate-500
    public const string Line = "#E2E8F0";     // slate-200
    public const string HeaderRow = "#F1F5F9";// slate-100
    public const string ZebraRow = "#F8FAFC"; // slate-50
    public const string Inflow = "#059669";   // emerald-600
    public const string Outflow = "#DC2626";  // red-600

    private static readonly TimeZoneInfo BrazilTimeZone = ResolveBrazilTimeZone();

    public static string Brl(decimal value) => value.ToString("C", Culture);

    public static string Date(DateOnly value) => value.ToString("dd/MM/yyyy", Culture);

    public static DateOnly TodayInBrazil(DateTime utcNow) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, BrazilTimeZone));

    public static string MonthReference(int month, int year)
    {
        var name = Culture.DateTimeFormat.GetMonthName(month);
        var capitalized = char.ToUpper(name[0], Culture) + name[1..];
        return $"{capitalized} de {year}";
    }

    public static string AccountType(FinancialAccountType type) => type switch
    {
        FinancialAccountType.CheckingAccount => "Conta corrente",
        FinancialAccountType.SavingsAccount => "Poupança",
        FinancialAccountType.Cash => "Dinheiro",
        FinancialAccountType.Other => "Outra",
        _ => type.ToString()
    };

    public static string MovementType(FinancialAccountMovementType type) => type switch
    {
        FinancialAccountMovementType.Income => "Receita",
        FinancialAccountMovementType.Expense => "Despesa",
        FinancialAccountMovementType.BillPayment => "Pagamento de conta",
        FinancialAccountMovementType.CreditCardStatementPayment => "Pagamento de fatura",
        FinancialAccountMovementType.TransferIn => "Transferência recebida",
        FinancialAccountMovementType.TransferOut => "Transferência enviada",
        FinancialAccountMovementType.Adjustment => "Ajuste",
        _ => type.ToString()
    };

    public static string StatementStatus(CreditCardStatementStatus status) => status switch
    {
        CreditCardStatementStatus.Open => "Aberta",
        CreditCardStatementStatus.Closed => "Fechada",
        CreditCardStatementStatus.Paid => "Paga",
        CreditCardStatementStatus.PartiallyPaid => "Parcialmente paga",
        CreditCardStatementStatus.Overdue => "Vencida",
        _ => status.ToString()
    };

    public static void Header(IContainer container, string title, string subtitle)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Osiris").FontSize(16).Bold().FontColor(Accent);
                row.AutoItem().AlignBottom().Text("Finanças pessoais").FontSize(9).FontColor(Muted);
            });

            column.Item().PaddingTop(10).Text(title).FontSize(18).Bold().FontColor(Ink);

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                column.Item().PaddingTop(2).Text(subtitle).FontSize(11).FontColor(Muted);
            }

            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Line);
        });
    }

    public static void Footer(IContainer container, string generatedOn)
    {
        container.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted)).Column(column =>
        {
            column.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(Line);
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(generatedOn);
                row.AutoItem().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });
    }

    public static void SummaryCard(RowDescriptor row, string label, string value, string valueColor)
    {
        row.RelativeItem()
            .Border(1).BorderColor(Line)
            .Padding(8)
            .Column(column =>
            {
                column.Item().Text(label.ToUpper(Culture)).FontSize(8).FontColor(Muted);
                column.Item().PaddingTop(2).Text(value).FontSize(13).Bold().FontColor(valueColor);
            });
    }

    public static void HeaderCell(IContainer cell, string text, bool alignRight = false)
    {
        var content = cell.Background(HeaderRow).PaddingVertical(5).PaddingHorizontal(6);
        if (alignRight)
        {
            content = content.AlignRight();
        }

        content.Text(text).FontSize(8).Bold().FontColor(Muted);
    }

    public static IContainer BodyCell(IContainer cell, bool zebra)
    {
        var content = cell.PaddingVertical(4).PaddingHorizontal(6);
        return zebra ? content.Background(ZebraRow) : content;
    }

    private static TimeZoneInfo ResolveBrazilTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
