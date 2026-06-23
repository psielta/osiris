using System.Globalization;
using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.CreditCardStatements.Queries.GetCreditCardStatementDetails;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>Write tool: proposes a payment against a credit card statement (fatura). Persists a proposal only.</summary>
public sealed class ProposeStatementPaymentTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeStatementPaymentTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_statement_payment";

    public string Description =>
        "Cria uma PROPOSTA de pagamento de fatura (liquida a dívida do cartão, não é uma nova despesa). NÃO "
        + "registra: o usuário confirma depois. Informe statementId; amount (padrão = saldo aberto), paidAt (ISO), "
        + "financialAccountId e notes são opcionais.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            statementId = new { type = "string", description = "Id da fatura (GUID)." },
            amount = new { type = "number", description = "Valor do pagamento. Opcional; padrão é o saldo aberto." },
            paidAt = new { type = "string", description = "Data do pagamento (ISO yyyy-MM-dd). Opcional; padrão hoje." },
            financialAccountId = new { type = "string", description = "Conta de onde sai o dinheiro (GUID). Opcional." },
            notes = new { type = "string", description = "Observações. Opcional." }
        },
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

        if (statement.OpenBalance <= 0m)
        {
            return AiToolResult.Failure("Esta fatura já está quitada.");
        }

        var amount = AiToolArguments.GetDecimal(arguments, "amount") ?? statement.OpenBalance;
        if (amount <= 0m)
        {
            return AiToolResult.Failure("Informe um amount maior que zero.");
        }

        var paidAt = AiToolArguments.GetDate(arguments, "paidAt") ?? context.Today;

        var payload = new StatementPaymentPayload(
            statement.Id,
            amount,
            paidAt,
            AiToolArguments.GetGuid(arguments, "financialAccountId"),
            AiToolArguments.GetString(arguments, "notes"));

        var stateHash = ProposalState.StatementHash(statement.OpenBalance, statement.Status);

        var reference = string.Create(
            CultureInfo.InvariantCulture,
            $"{statement.ReferenceMonth:D2}/{statement.ReferenceYear}");
        var display = $"Pagar {ProposalFormatting.Money(amount)} da fatura {reference} ({statement.CreditCardName})";
        var impact = $"Reduz o saldo aberto da fatura (atual: {ProposalFormatting.Money(statement.OpenBalance)}).";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.StatementPayment, payload, display, impact, stateHash, cancellationToken);
    }
}
