using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.CreditCards.Queries.ListCreditCards;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Lists the tenant's credit cards with limit and closing/due days.</summary>
public sealed class ListCreditCardsTool : IAiTool
{
    private readonly ISender _sender;

    public ListCreditCardsTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "list_credit_cards";

    public string Description => "Lista os cartões de crédito do usuário com limite e dias de fechamento e vencimento.";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            includeArchived = new { type = "boolean", description = "Incluir cartões arquivados. Padrão: false." }
        }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var includeArchived = AiToolArguments.GetBool(arguments, "includeArchived");

        var cards = await _sender.Send(new ListCreditCardsQuery(includeArchived), cancellationToken);

        var payload = new
        {
            cards = cards.Select(card => new
            {
                card.Id,
                card.Name,
                card.Limit,
                card.ClosingDay,
                card.DueDay,
                card.IsActive
            })
        };

        var sources = cards.Select(card => new AiSource("creditCard", card.Id.ToString(), card.Name)).ToList();
        return AiToolResult.Success(AiToolJson.Serialize(payload), sources);
    }
}
