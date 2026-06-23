using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.CreditCards.DTOs;
using Osiris.Application.Features.CreditCards.Queries.GetCreditCardForEdit;
using Osiris.Application.Features.CreditCards.Queries.ListCreditCards;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>Write tool: proposes creating a credit card. Persists a proposal only.</summary>
public sealed class ProposeCardCreationTool : IAiTool
{
    private readonly IAiActionProposalFactory _factory;

    public ProposeCardCreationTool(IAiActionProposalFactory factory) => _factory = factory;

    public string Name => "propose_card_creation";

    public string Description =>
        "Cria uma PROPOSTA de novo cartão de crédito. NÃO registra: o usuário confirma depois. Informe name, limit, "
        + "closingDay (dia de fechamento, 1-31) e dueDay (dia de vencimento, 1-31); paymentAccountId é opcional.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string", description = "Nome do cartão." },
            limit = new { type = "number", description = "Limite do cartão." },
            closingDay = new { type = "integer", description = "Dia de fechamento (1-31)." },
            dueDay = new { type = "integer", description = "Dia de vencimento (1-31)." },
            paymentAccountId = new { type = "string", description = "Conta de pagamento padrão (GUID). Opcional." }
        },
        required = new[] { "name", "limit", "closingDay", "dueDay" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var name = AiToolArguments.GetString(arguments, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return AiToolResult.Failure("Informe o nome do cartão.");
        }

        var limit = AiToolArguments.GetDecimal(arguments, "limit");
        if (limit is null or < 0m)
        {
            return AiToolResult.Failure("Informe um limit válido (>= 0).");
        }

        var closingDay = AiToolArguments.GetInt(arguments, "closingDay");
        var dueDay = AiToolArguments.GetInt(arguments, "dueDay");
        if (closingDay is null or < 1 or > 31 || dueDay is null or < 1 or > 31)
        {
            return AiToolResult.Failure("Informe closingDay e dueDay entre 1 e 31.");
        }

        var payload = new CardCreationPayload(
            name.Trim(), limit.Value, closingDay.Value, dueDay.Value, AiToolArguments.GetGuid(arguments, "paymentAccountId"));
        var display = $"Criar cartão \"{name.Trim()}\" (limite {ProposalFormatting.Money(limit.Value)}, fecha dia {closingDay}, vence dia {dueDay})";
        var impact = "Adiciona um novo cartão de crédito.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.CardCreation, payload, display, impact, cancellationToken);
    }
}

/// <summary>Write tool: proposes editing a credit card. Persists a proposal only.</summary>
public sealed class ProposeCardUpdateTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeCardUpdateTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_card_update";

    public string Description =>
        "Cria uma PROPOSTA de edição de um cartão (nome, limite, dias de fechamento/vencimento). NÃO registra: o "
        + "usuário confirma depois. Informe creditCardId (de list_credit_cards); name, limit, closingDay e dueDay "
        + "são opcionais (o que não for informado permanece igual).";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            creditCardId = new { type = "string", description = "Id do cartão (GUID)." },
            name = new { type = "string", description = "Novo nome. Opcional." },
            limit = new { type = "number", description = "Novo limite. Opcional." },
            closingDay = new { type = "integer", description = "Novo dia de fechamento (1-31). Opcional." },
            dueDay = new { type = "integer", description = "Novo dia de vencimento (1-31). Opcional." }
        },
        required = new[] { "creditCardId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var cardId = AiToolArguments.GetGuid(arguments, "creditCardId");
        if (cardId is null)
        {
            return AiToolResult.Failure("Informe um creditCardId válido.");
        }

        var current = await _sender.Send(new GetCreditCardForEditQuery(cardId.Value), cancellationToken);
        if (current is null)
        {
            return AiToolResult.Failure("Cartão não encontrado.");
        }

        var name = AiToolArguments.GetString(arguments, "name")?.Trim();
        var finalName = string.IsNullOrWhiteSpace(name) ? current.Name : name;
        var finalLimit = AiToolArguments.GetDecimal(arguments, "limit") ?? current.Limit;
        var finalClosing = AiToolArguments.GetInt(arguments, "closingDay") ?? current.ClosingDay;
        var finalDue = AiToolArguments.GetInt(arguments, "dueDay") ?? current.DueDay;

        if (finalLimit < 0m || finalClosing is < 1 or > 31 || finalDue is < 1 or > 31)
        {
            return AiToolResult.Failure("Limite deve ser >= 0 e os dias entre 1 e 31.");
        }

        var payload = new CardUpdatePayload(current.Id, finalName, finalLimit, finalClosing, finalDue, current.PaymentAccountId);
        var stateHash = ProposalState.CardHash(current.Name, current.Limit, current.ClosingDay, current.DueDay);
        var display = $"Editar o cartão \"{current.Name}\" (nome: \"{finalName}\", limite {ProposalFormatting.Money(finalLimit)}, fecha {finalClosing}, vence {finalDue})";
        var impact = "Atualiza o cadastro do cartão; faturas e compras existentes não mudam.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.CardUpdate, payload, display, impact, stateHash, cancellationToken);
    }
}

/// <summary>Write tool: proposes archiving (hiding) a credit card. Persists a proposal only.</summary>
public sealed class ProposeCardArchiveTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeCardArchiveTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_card_archive";

    public string Description =>
        "Cria uma PROPOSTA de arquivamento de um cartão (deixa de aparecer para novas compras, sem apagar o "
        + "histórico). NÃO registra: o usuário confirma depois. Informe creditCardId.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new { creditCardId = new { type = "string", description = "Id do cartão (GUID)." } },
        required = new[] { "creditCardId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var cardId = AiToolArguments.GetGuid(arguments, "creditCardId");
        if (cardId is null)
        {
            return AiToolResult.Failure("Informe um creditCardId válido.");
        }

        var cards = await _sender.Send(new ListCreditCardsQuery(IncludeArchived: true), cancellationToken);
        var current = cards.FirstOrDefault(card => card.Id == cardId.Value);
        if (current is null)
        {
            return AiToolResult.Failure("Cartão não encontrado.");
        }

        if (!current.IsActive)
        {
            return AiToolResult.Failure("O cartão já está arquivado.");
        }

        var payload = new CardRefPayload(current.Id);
        var stateHash = ProposalState.CardHash(current.Name, current.Limit, current.ClosingDay, current.DueDay);
        var display = $"Arquivar o cartão \"{current.Name}\"";
        var impact = "O cartão deixa de aparecer para novas compras; o histórico de faturas é preservado.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.CardArchive, payload, display, impact, stateHash, cancellationToken);
    }
}
