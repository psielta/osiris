using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.FinancialAccountMovements.Queries.ListMovements;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>
/// Searches the movements of a single account by period, category, text and minimum amount. Scoped to
/// one account (the caller provides accountId); a cross-account search would need a dedicated query.
/// </summary>
public sealed class SearchAccountMovementsTool : IAiTool
{
    private const int MaxMovements = 50;

    private readonly ISender _sender;

    public SearchAccountMovementsTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "search_account_movements";

    public string Description =>
        "Busca lançamentos de uma conta por período, categoria, texto na descrição e valor mínimo. "
        + "Informe o accountId (de list_financial_accounts).";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            accountId = new { type = "string", description = "Id da conta (GUID)." },
            from = new { type = "string", description = "Início do período, ISO yyyy-MM-dd. Opcional." },
            to = new { type = "string", description = "Fim do período, ISO yyyy-MM-dd. Opcional." },
            categoryId = new { type = "string", description = "Filtra por categoria (GUID). Opcional." },
            text = new { type = "string", description = "Texto contido na descrição. Opcional." },
            minAmount = new { type = "number", description = "Valor mínimo (absoluto). Opcional." }
        },
        required = new[] { "accountId" }
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

        var movements = await _sender.Send(new ListMovementsQuery(accountId.Value), cancellationToken);

        var from = AiToolArguments.GetDate(arguments, "from");
        var to = AiToolArguments.GetDate(arguments, "to");
        var categoryId = AiToolArguments.GetGuid(arguments, "categoryId");
        var text = AiToolArguments.GetString(arguments, "text");
        var minAmount = AiToolArguments.GetDecimal(arguments, "minAmount");

        var filtered = movements
            .Where(movement => (from is null || movement.OccurredOn >= from)
                && (to is null || movement.OccurredOn <= to)
                && (categoryId is null || movement.CategoryId == categoryId)
                && (minAmount is null || movement.Amount >= minAmount)
                && (string.IsNullOrWhiteSpace(text)
                    || movement.Description.Contains(text, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(movement => movement.OccurredOn)
            .Take(MaxMovements)
            .Select(movement => new
            {
                movement.Id,
                movement.OccurredOn,
                movement.Description,
                movement.Amount,
                type = movement.Type.ToString(),
                movement.IsInflow
            })
            .ToList();

        var payload = new
        {
            accountId = accountId.Value,
            movements = filtered,
            movementCount = filtered.Count
        };

        return AiToolResult.Success(AiToolJson.Serialize(payload));
    }
}
