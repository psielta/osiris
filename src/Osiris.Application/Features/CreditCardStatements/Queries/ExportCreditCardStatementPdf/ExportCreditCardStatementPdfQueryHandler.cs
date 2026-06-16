using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Common.Text;
using Osiris.Application.Features.CreditCardStatements.Queries.GetCreditCardStatementDetails;

namespace Osiris.Application.Features.CreditCardStatements.Queries.ExportCreditCardStatementPdf;

public sealed class ExportCreditCardStatementPdfQueryHandler
    : IRequestHandler<ExportCreditCardStatementPdfQuery, FileExportResult?>
{
    private readonly ISender _sender;
    private readonly ICreditCardStatementPdfRenderer _renderer;

    public ExportCreditCardStatementPdfQueryHandler(
        ISender sender,
        ICreditCardStatementPdfRenderer renderer)
    {
        _sender = sender;
        _renderer = renderer;
    }

    public async Task<FileExportResult?> Handle(
        ExportCreditCardStatementPdfQuery request,
        CancellationToken cancellationToken)
    {
        // Reuse the same query that feeds the on-screen statement so the PDF never drifts from it.
        var statement = await _sender.Send(new GetCreditCardStatementDetailsQuery(request.StatementId), cancellationToken);

        // Mirror the controller's ownership guard: the statement must belong to the card in the route.
        if (statement is null || statement.CreditCardId != request.CreditCardId)
        {
            return null;
        }

        var content = _renderer.Render(statement);
        var fileName = $"fatura-{Slug.From(statement.CreditCardName)}-{statement.ReferenceYear:0000}-{statement.ReferenceMonth:00}.pdf";
        return new FileExportResult(content, fileName);
    }
}
