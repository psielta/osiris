using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Common.Csv;

namespace Osiris.Web.Models;

/// <summary>
/// Backs the CSV column-mapping screen. The file bytes ride along as Base64 in a hidden field so the
/// mapping can be re-applied (refresh sample / preview) without re-uploading. <see cref="SampleRows"/>
/// is display-only and is repopulated from the bytes on every render.
/// </summary>
public sealed class CsvImportMappingViewModel
{
    public Guid AccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileContentBase64 { get; set; } = string.Empty;

    public string Delimiter { get; set; } = ";";

    public string Encoding { get; set; } = "utf-8";

    public bool HasHeader { get; set; } = true;

    public int HeaderLineIndex { get; set; }

    public CsvAmountMode AmountMode { get; set; } = CsvAmountMode.SignedAmount;

    public int DateColumn { get; set; }

    public int DescriptionColumn { get; set; }

    public int? SecondaryDescriptionColumn { get; set; }

    public int? AmountColumn { get; set; }

    public int? CreditColumn { get; set; }

    public int? DebitColumn { get; set; }

    public int? TypeColumn { get; set; }

    public int? ExternalIdColumn { get; set; }

    public string DateFormat { get; set; } = "dd/MM/yyyy";

    public string DecimalSeparator { get; set; } = ",";

    public IReadOnlyList<IReadOnlyList<string>> SampleRows { get; set; } = Array.Empty<IReadOnlyList<string>>();

    public IReadOnlyList<string> Columns
    {
        get
        {
            if (HasHeader && HeaderLineIndex >= 0 && HeaderLineIndex < SampleRows.Count)
            {
                return SampleRows[HeaderLineIndex]
                    .Select((cell, index) => string.IsNullOrWhiteSpace(cell) ? $"Coluna {index + 1}" : cell.Trim())
                    .ToList();
            }

            var width = SampleRows.Count > 0 ? SampleRows.Max(row => row.Count) : 0;
            return Enumerable.Range(0, width).Select(index => $"Coluna {index + 1}").ToList();
        }
    }

    public IEnumerable<SelectListItem> ColumnOptions =>
        Columns.Select((label, index) => new SelectListItem($"{index + 1} — {label}", index.ToString()));

    public IEnumerable<SelectListItem> HeaderLineOptions =>
        SampleRows.Select((row, index) => new SelectListItem(
            $"Linha {index + 1}: {Truncate(string.Join(" | ", row), 70)}",
            index.ToString()));

    public CsvImportMapping ToMapping() => new()
    {
        Delimiter = Delimiter,
        Encoding = Encoding,
        HasHeader = HasHeader,
        HeaderLineIndex = HeaderLineIndex,
        AmountMode = AmountMode,
        DateColumn = DateColumn,
        DescriptionColumn = DescriptionColumn,
        SecondaryDescriptionColumn = SecondaryDescriptionColumn,
        AmountColumn = AmountColumn,
        CreditColumn = CreditColumn,
        DebitColumn = DebitColumn,
        TypeColumn = TypeColumn,
        ExternalIdColumn = ExternalIdColumn,
        DateFormat = DateFormat,
        DecimalSeparator = DecimalSeparator
    };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
