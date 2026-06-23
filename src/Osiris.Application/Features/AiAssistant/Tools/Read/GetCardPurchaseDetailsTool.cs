using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Returns a single credit card purchase with its installment plan (one row per installment).</summary>
public sealed class GetCardPurchaseDetailsTool : IAiTool
{
    private readonly ISender _sender;

    public GetCardPurchaseDetailsTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "get_card_purchase_details";

    public string Description =>
        "Retorna os detalhes de uma compra no cartão, incluindo o parcelamento (uma linha por parcela, com "
        + "valor, vencimento e a fatura de cada parcela). Informe o creditCardPurchaseId (de search_card_purchases).";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new { creditCardPurchaseId = new { type = "string", description = "Id da compra (GUID)." } },
        required = new[] { "creditCardPurchaseId" }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
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

        var payload = new
        {
            purchase = new
            {
                purchase.Id,
                creditCard = purchase.CreditCardName,
                category = purchase.CategoryName,
                purchase.Description,
                purchase.TotalAmount,
                purchase.PurchaseDate,
                purchase.Installments,
                purchase.Notes,
                installmentItems = purchase.InstallmentItems.Select(item => new
                {
                    item.InstallmentNumber,
                    item.TotalInstallments,
                    item.Amount,
                    item.DueDate,
                    item.ReferenceMonth,
                    item.ReferenceYear
                })
            }
        };

        var sources = new List<AiSource> { new("creditCardPurchase", purchase.Id.ToString(), purchase.Description) };
        return AiToolResult.Success(AiToolJson.Serialize(payload), sources);
    }
}
