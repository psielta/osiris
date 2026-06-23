using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Returns an account's balances and its movements within an optional period.</summary>
public sealed class GetAccountStatementTool : IAiTool
{
    private const int MaxMovements = 50;

    private readonly ISender _sender;

    public GetAccountStatementTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "get_account_statement";

    public string Description =>
        "Retorna o extrato de uma conta: saldo e os lançamentos no período informado. "
        + "Informe o accountId (obtido em list_financial_accounts).";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            accountId = new { type = "string", description = "Id da conta (GUID)." },
            from = new { type = "string", description = "Início do período, ISO yyyy-MM-dd. Opcional." },
            to = new { type = "string", description = "Fim do período, ISO yyyy-MM-dd. Opcional." }
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

        var statement = await _sender.Send(new GetFinancialAccountDetailsQuery(accountId.Value), cancellationToken);
        if (statement is null)
        {
            return AiToolResult.Failure("Conta não encontrada.");
        }

        var from = AiToolArguments.GetDate(arguments, "from");
        var to = AiToolArguments.GetDate(arguments, "to");

        var movements = statement.Movements
            .Where(movement => (from is null || movement.OccurredOn >= from)
                && (to is null || movement.OccurredOn <= to))
            .OrderByDescending(movement => movement.OccurredOn)
            .Take(MaxMovements)
            .Select(movement => new
            {
                movement.OccurredOn,
                movement.Description,
                movement.Amount,
                type = movement.Type.ToString(),
                movement.IsInflow
            })
            .ToList();

        var payload = new
        {
            account = new
            {
                statement.Id,
                statement.Name,
                type = statement.Type.ToString(),
                statement.CurrentBalance
            },
            period = new { from, to },
            movements,
            movementCount = movements.Count,
            truncated = statement.Movements.Count > movements.Count
        };

        var sources = new List<AiSource> { new("account", statement.Id.ToString(), statement.Name) };
        return AiToolResult.Success(AiToolJson.Serialize(payload), sources);
    }
}
