using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.CreditCards.Queries.ListCreditCards;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>Write tool: proposes a credit card purchase (optionally installments). Persists a proposal only.</summary>
public sealed class ProposeCardPurchaseTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeCardPurchaseTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_card_purchase";

    public string Description =>
        "Cria uma PROPOSTA de compra no cartão (a compra é a despesa categorizada). NÃO registra: o usuário "
        + "confirma depois. Informe creditCardId, description e totalAmount; purchaseDate (ISO), installments, "
        + "categoryId e notes são opcionais.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            creditCardId = new { type = "string", description = "Id do cartão (GUID)." },
            description = new { type = "string", description = "Descrição da compra." },
            totalAmount = new { type = "number", description = "Valor total da compra (positivo)." },
            purchaseDate = new { type = "string", description = "Data da compra (ISO yyyy-MM-dd). Opcional; padrão hoje." },
            installments = new { type = "integer", description = "Número de parcelas. Opcional; padrão 1." },
            categoryId = new { type = "string", description = "Categoria (GUID). Opcional." },
            notes = new { type = "string", description = "Observações. Opcional." }
        },
        required = new[] { "creditCardId", "description", "totalAmount" }
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

        var description = AiToolArguments.GetString(arguments, "description");
        if (string.IsNullOrWhiteSpace(description))
        {
            return AiToolResult.Failure("Informe a description da compra.");
        }

        var totalAmount = AiToolArguments.GetDecimal(arguments, "totalAmount");
        if (totalAmount is null or <= 0m)
        {
            return AiToolResult.Failure("Informe um totalAmount maior que zero.");
        }

        var cards = await _sender.Send(new ListCreditCardsQuery(IncludeArchived: false), cancellationToken);
        var card = cards.FirstOrDefault(item => item.Id == creditCardId);
        if (card is null)
        {
            return AiToolResult.Failure("Cartão não encontrado ou arquivado.");
        }

        var installments = AiToolArguments.GetInt(arguments, "installments") ?? 1;
        if (installments < 1)
        {
            installments = 1;
        }

        var purchaseDate = AiToolArguments.GetDate(arguments, "purchaseDate") ?? context.Today;

        var payload = new CardPurchasePayload(
            card.Id,
            AiToolArguments.GetGuid(arguments, "categoryId"),
            description.Trim(),
            totalAmount.Value,
            purchaseDate,
            installments,
            AiToolArguments.GetString(arguments, "notes"));

        var display = $"Registrar compra \"{description.Trim()}\" de {ProposalFormatting.Money(totalAmount.Value)} "
            + $"em {installments}x no cartão {card.Name}";
        var impact = $"A compra entrará nas faturas do cartão {card.Name}.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.CardPurchase, payload, display, impact, cancellationToken);
    }
}
