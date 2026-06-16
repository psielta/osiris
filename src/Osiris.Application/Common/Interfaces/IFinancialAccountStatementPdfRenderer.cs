using Osiris.Application.Features.FinancialAccounts.DTOs;

namespace Osiris.Application.Common.Interfaces;

/// <summary>
/// Renders an account statement ("extrato") into a PDF document. Implemented in Infrastructure
/// so the rendering technology (QuestPDF) stays out of the Application and Web layers.
/// </summary>
public interface IFinancialAccountStatementPdfRenderer
{
    byte[] Render(FinancialAccountStatementDto statement);
}
