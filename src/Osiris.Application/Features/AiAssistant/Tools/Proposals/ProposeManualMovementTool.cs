using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>
/// Write tool: turns a request to register a manual income/expense into a confirmable
/// <see cref="AiActionProposal"/>. It validates the target account and persists the proposal, but never
/// creates the movement — that only happens when the user confirms.
/// </summary>
public sealed class ProposeManualMovementTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalRepository _proposals;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AiAgentOptions _options;

    public ProposeManualMovementTool(
        ISender sender,
        IAiActionProposalRepository proposals,
        IDateTimeProvider dateTimeProvider,
        IOptions<AiAgentOptions> options)
    {
        _sender = sender;
        _proposals = proposals;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
    }

    public string Name => "propose_manual_movement";

    public string Description =>
        "Cria uma PROPOSTA de lançamento manual (entrada ou saída) em uma conta. NÃO registra nada: "
        + "o usuário precisa confirmar depois. Informe accountId, type ('income' ou 'expense'), amount, "
        + "description e, opcionalmente, occurredOn (ISO), categoryId e notes.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            accountId = new { type = "string", description = "Id da conta (GUID)." },
            type = new { type = "string", @enum = new[] { "income", "expense" }, description = "Tipo do lançamento." },
            amount = new { type = "number", description = "Valor positivo do lançamento." },
            description = new { type = "string", description = "Descrição do lançamento." },
            occurredOn = new { type = "string", description = "Data do lançamento (ISO yyyy-MM-dd). Opcional; padrão hoje." },
            categoryId = new { type = "string", description = "Categoria (GUID). Opcional." },
            notes = new { type = "string", description = "Observações. Opcional." }
        },
        required = new[] { "accountId", "type", "amount", "description" }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var accountId = AiToolArguments.GetGuid(arguments, "accountId");
        if (accountId is null)
        {
            return AiToolResult.Failure("Informe um accountId válido.");
        }

        var movementType = ParseType(AiToolArguments.GetString(arguments, "type"));
        if (movementType is null)
        {
            return AiToolResult.Failure("Informe type como 'income' (entrada) ou 'expense' (saída).");
        }

        var amount = AiToolArguments.GetDecimal(arguments, "amount");
        if (amount is null or <= 0m)
        {
            return AiToolResult.Failure("Informe um amount maior que zero.");
        }

        var description = AiToolArguments.GetString(arguments, "description");
        if (string.IsNullOrWhiteSpace(description))
        {
            return AiToolResult.Failure("Informe a description do lançamento.");
        }

        var account = await _sender.Send(new GetFinancialAccountDetailsQuery(accountId.Value), cancellationToken);
        if (account is null || !account.IsActive)
        {
            return AiToolResult.Failure("Conta não encontrada ou arquivada.");
        }

        var occurredOn = AiToolArguments.GetDate(arguments, "occurredOn") ?? context.Today;
        var categoryId = AiToolArguments.GetGuid(arguments, "categoryId");
        var notes = AiToolArguments.GetString(arguments, "notes");

        var payload = new ManualMovementPayload(
            account.Id,
            movementType.Value.ToString(),
            amount.Value,
            occurredOn,
            description.Trim(),
            categoryId,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());

        var isExpense = movementType.Value == FinancialAccountMovementType.Expense;
        var projectedBalance = isExpense ? account.CurrentBalance - amount.Value : account.CurrentBalance + amount.Value;
        var verb = isExpense ? "Registrar saída" : "Registrar entrada";

        var displaySummary = $"{verb} de {ProposalFormatting.Money(amount.Value)} em \"{account.Name}\"";
        var impactSummary =
            $"O saldo da conta passará de {ProposalFormatting.Money(account.CurrentBalance)} para {ProposalFormatting.Money(projectedBalance)}.";

        var now = _dateTimeProvider.UtcNow;
        var proposal = new AiActionProposal(
            context.TenantId,
            context.UserId,
            context.ConversationId,
            AiActionTypes.ManualMovement,
            AiToolJson.Serialize(payload),
            displaySummary,
            impactSummary,
            AiToolRisk.WriteProposal,
            Guid.NewGuid().ToString("n"),
            ProposalState.AccountHash(account.CurrentBalance, account.IsActive),
            now,
            now.AddMinutes(_options.ProposalTtlMinutes));

        await _proposals.AddAsync(proposal, cancellationToken);

        var resultJson = AiToolJson.Serialize(new
        {
            proposalId = proposal.Id,
            status = "pending_confirmation",
            action = AiActionTypes.ManualMovement,
            display = displaySummary,
            impact = impactSummary,
            note = "A proposta foi criada e aguarda confirmação do usuário. Não foi registrada ainda."
        });

        var reference = new AiProposalReference(
            proposal.Id,
            AiActionTypes.ManualMovement,
            displaySummary,
            impactSummary,
            AiToolRisk.WriteProposal.ToString(),
            proposal.ExpiresAtUtc);

        return AiToolResult.Success(resultJson, sources: null, proposals: new[] { reference });
    }

    private static FinancialAccountMovementType? ParseType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "income" or "entrada" or "receita" => FinancialAccountMovementType.Income,
            "expense" or "saida" or "saída" or "despesa" => FinancialAccountMovementType.Expense,
            _ => null
        };
    }
}
