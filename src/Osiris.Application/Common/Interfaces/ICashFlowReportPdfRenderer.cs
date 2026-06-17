using Osiris.Application.Features.Reports.DTOs;

namespace Osiris.Application.Common.Interfaces;

/// <summary>
/// Renders cash-flow reports into PDF documents. Implemented in Infrastructure so QuestPDF stays
/// outside the Application layer.
/// </summary>
public interface ICashFlowReportPdfRenderer
{
    byte[] Render(CashFlowReportDto report);
}
