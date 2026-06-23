using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.CreditCardPurchases.Queries.ListAllCreditCardPurchases;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Searches credit card purchases across cards by period, card, category text and minimum amount.</summary>
public sealed class SearchCardPurchasesTool : IAiTool
{
    private const int MaxPurchases = 50;

    private readonly ISender _sender;

    public SearchCardPurchasesTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "search_card_purchases";

    public string Description =>
        "Busca compras no cartão por período, cartão, categoria e valor mínimo. A compra é a despesa "
        + "categorizada (não confunda com o pagamento da fatura).";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            from = new { type = "string", description = "Início do período, ISO yyyy-MM-dd. Opcional." },
            to = new { type = "string", description = "Fim do período, ISO yyyy-MM-dd. Opcional." },
            creditCardId = new { type = "string", description = "Filtra por cartão (GUID). Opcional." },
            categoryName = new { type = "string", description = "Texto contido no nome da categoria. Opcional." },
            minAmount = new { type = "number", description = "Valor total mínimo da compra. Opcional." }
        }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var from = AiToolArguments.GetDate(arguments, "from");
        var to = AiToolArguments.GetDate(arguments, "to");
        var creditCardId = AiToolArguments.GetGuid(arguments, "creditCardId");
        var categoryName = AiToolArguments.GetString(arguments, "categoryName");
        var minAmount = AiToolArguments.GetDecimal(arguments, "minAmount");

        var purchases = await _sender.Send(new ListAllCreditCardPurchasesQuery(from, to), cancellationToken);

        var filtered = purchases
            .Where(purchase => (creditCardId is null || purchase.CreditCardId == creditCardId)
                && (minAmount is null || purchase.TotalAmount >= minAmount)
                && (string.IsNullOrWhiteSpace(categoryName)
                    || (purchase.CategoryName is not null
                        && purchase.CategoryName.Contains(categoryName, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(purchase => purchase.PurchaseDate)
            .Take(MaxPurchases)
            .Select(purchase => new
            {
                purchase.Id,
                purchase.Description,
                creditCard = purchase.CreditCardName,
                category = purchase.CategoryName,
                purchase.TotalAmount,
                purchase.PurchaseDate,
                purchase.Installments
            })
            .ToList();

        var payload = new
        {
            period = new { from, to },
            purchases = filtered,
            purchaseCount = filtered.Count
        };

        return AiToolResult.Success(AiToolJson.Serialize(payload));
    }
}
