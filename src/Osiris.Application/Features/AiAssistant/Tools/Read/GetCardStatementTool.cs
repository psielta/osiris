using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Application.Features.CreditCardStatements.Queries.GetCurrentCreditCardStatement;
using Osiris.Application.Features.CreditCardStatements.Queries.ListCreditCardStatements;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>
/// Returns a card's statement for a given competence (or the current cycle when none is given):
/// totals, due/closing dates and status.
/// </summary>
public sealed class GetCardStatementTool : IAiTool
{
    private readonly ISender _sender;

    public GetCardStatementTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "get_card_statement";

    public string Description =>
        "Retorna a fatura de UM cartão específico (totais, datas de fechamento/vencimento e status). Informe o "
        + "creditCardId (de list_credit_cards); referenceMonth/referenceYear são opcionais (padrão: fatura atual). "
        + "Para somar ou comparar as faturas de todos os cartões, use get_statements_overview em vez de chamar "
        + "esta ferramenta cartão por cartão.";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            creditCardId = new { type = "string", description = "Id do cartão (GUID)." },
            referenceMonth = new { type = "integer", description = "Mês de competência (1-12). Opcional." },
            referenceYear = new { type = "integer", description = "Ano de competência. Opcional." }
        },
        required = new[] { "creditCardId" }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var creditCardId = AiToolArguments.GetGuid(arguments, "creditCardId");
        if (creditCardId is null)
        {
            return AiToolResult.Failure("Informe um creditCardId válido.");
        }

        var referenceMonth = AiToolArguments.GetInt(arguments, "referenceMonth");
        var referenceYear = AiToolArguments.GetInt(arguments, "referenceYear");

        CreditCardStatementListItemDto? statement;
        if (referenceMonth is >= 1 and <= 12 && referenceYear is > 0)
        {
            var statements = await _sender.Send(
                new ListCreditCardStatementsQuery(creditCardId.Value),
                cancellationToken);
            statement = statements.FirstOrDefault(item =>
                item.ReferenceMonth == referenceMonth && item.ReferenceYear == referenceYear);
        }
        else
        {
            statement = await _sender.Send(
                new GetCurrentCreditCardStatementQuery(creditCardId.Value),
                cancellationToken);
        }

        if (statement is null)
        {
            return AiToolResult.Failure("Fatura não encontrada para o cartão e período informados.");
        }

        var payload = new
        {
            statement = new
            {
                statement.Id,
                statement.CreditCardId,
                statement.ReferenceMonth,
                statement.ReferenceYear,
                statement.ClosingDate,
                statement.DueDate,
                status = statement.Status.ToString(),
                statement.TotalAmount,
                statement.PaidAmount,
                statement.OpenBalance
            }
        };

        var sources = new List<AiSource> { new("creditCardStatement", statement.Id.ToString(), $"Fatura {statement.ReferenceMonth:D2}/{statement.ReferenceYear}") };
        return AiToolResult.Success(AiToolJson.Serialize(payload), sources);
    }
}
