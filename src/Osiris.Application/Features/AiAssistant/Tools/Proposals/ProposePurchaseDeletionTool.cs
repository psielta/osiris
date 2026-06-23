using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>Write tool: proposes deleting a credit card purchase. Persists a proposal only.</summary>
public sealed class ProposePurchaseDeletionTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposePurchaseDeletionTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_purchase_deletion";

    public string Description =>
        "Cria uma PROPOSTA de EXCLUSÃO de uma compra no cartão. Ação irreversível; não é possível se a fatura da "
        + "compra já estiver paga. NÃO registra: o usuário confirma depois. Informe creditCardPurchaseId "
        + "(de search_card_purchases).";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new { creditCardPurchaseId = new { type = "string", description = "Id da compra (GUID)." } },
        required = new[] { "creditCardPurchaseId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var purchaseId = AiToolArguments.GetGuid(arguments, "creditCardPurchaseId");
        if (purchaseId is null)
        {
            return AiToolResult.Failure("Informe um creditCardPurchaseId válido.");
        }

        var purchase = await _sender.Send(new GetCreditCardPurchaseDetailsQuery(purchaseId.Value), cancellationToken);
        if (purchase is null)
        {
            return AiToolResult.Failure("Compra não encontrada.");
        }

        var payload = new PurchaseRefPayload(purchase.Id);
        var stateHash = ProposalState.PurchaseHash(purchase.TotalAmount);
        var display = $"Excluir a compra \"{purchase.Description}\" de {ProposalFormatting.Money(purchase.TotalAmount)} no cartão {purchase.CreditCardName}";
        var impact = "Ação irreversível. Não é possível excluir se a fatura da compra já estiver paga.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.PurchaseDeletion, payload, display, impact, stateHash, cancellationToken);
    }
}
