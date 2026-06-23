using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.CreditCardStatements.Queries.GetCreditCardStatementDetails;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Returns a statement (fatura) with the purchases/installments that compose it and its payments.</summary>
public sealed class GetCardStatementDetailsTool : IAiTool
{
    private const int MaxItems = 80;

    private readonly ISender _sender;

    public GetCardStatementDetailsTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "get_card_statement_details";

    public string Description =>
        "Retorna o detalhamento de uma fatura: cada parcela/compra que a compõe (descrição, nº da parcela e valor) "
        + "e os pagamentos já feitos. Informe o statementId (de get_statements_overview ou get_card_statement).";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new { statementId = new { type = "string", description = "Id da fatura (GUID)." } },
        required = new[] { "statementId" }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var statementId = AiToolArguments.GetGuid(arguments, "statementId");
        if (statementId is null)
        {
            return AiToolResult.Failure("Informe um statementId válido.");
        }

        var statement = await _sender.Send(new GetCreditCardStatementDetailsQuery(statementId.Value), cancellationToken);
        if (statement is null)
        {
            return AiToolResult.Failure("Fatura não encontrada.");
        }

        var payload = new
        {
            statement = new
            {
                statement.Id,
                creditCard = statement.CreditCardName,
                statement.ReferenceMonth,
                statement.ReferenceYear,
                statement.ClosingDate,
                statement.DueDate,
                status = statement.Status.ToString(),
                statement.TotalAmount,
                statement.PaidAmount,
                statement.OpenBalance,
                items = statement.InstallmentItems.Take(MaxItems).Select(item => new
                {
                    item.PurchaseDescription,
                    item.InstallmentNumber,
                    item.TotalInstallments,
                    item.Amount
                }),
                payments = statement.Payments.Select(payment => new
                {
                    payment.Amount,
                    payment.PaidAt,
                    account = payment.FinancialAccountName,
                    payment.Notes
                })
            }
        };

        var sources = new List<AiSource>
        {
            new("creditCardStatement", statement.Id.ToString(), $"Fatura {statement.CreditCardName} {statement.ReferenceMonth:D2}/{statement.ReferenceYear}")
        };
        return AiToolResult.Success(AiToolJson.Serialize(payload), sources);
    }
}
