using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Common.Text;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;

namespace Osiris.Application.Features.FinancialAccounts.Queries.ExportFinancialAccountStatementPdf;

public sealed class ExportFinancialAccountStatementPdfQueryHandler
    : IRequestHandler<ExportFinancialAccountStatementPdfQuery, FileExportResult?>
{
    private readonly ISender _sender;
    private readonly IFinancialAccountStatementPdfRenderer _renderer;

    public ExportFinancialAccountStatementPdfQueryHandler(
        ISender sender,
        IFinancialAccountStatementPdfRenderer renderer)
    {
        _sender = sender;
        _renderer = renderer;
    }

    public async Task<FileExportResult?> Handle(
        ExportFinancialAccountStatementPdfQuery request,
        CancellationToken cancellationToken)
    {
        // Reuse the same query that feeds the on-screen statement so the PDF never drifts from it.
        var statement = await _sender.Send(new GetFinancialAccountDetailsQuery(request.AccountId), cancellationToken);
        if (statement is null)
        {
            return null;
        }

        var content = _renderer.Render(statement);
        var fileName = $"extrato-{Slug.From(statement.Name)}.pdf";
        return new FileExportResult(content, fileName);
    }
}
