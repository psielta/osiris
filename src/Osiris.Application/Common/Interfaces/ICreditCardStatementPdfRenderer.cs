using Osiris.Application.Features.CreditCardStatements.DTOs;

namespace Osiris.Application.Common.Interfaces;

/// <summary>
/// Renders a credit-card statement ("fatura") into a PDF document. Implemented in Infrastructure
/// so the rendering technology (QuestPDF) stays out of the Application and Web layers.
/// </summary>
public interface ICreditCardStatementPdfRenderer
{
    byte[] Render(CreditCardStatementDetailsDto statement);
}
