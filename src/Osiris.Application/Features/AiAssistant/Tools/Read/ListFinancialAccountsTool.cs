using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Lists the tenant's financial accounts with their current balances.</summary>
public sealed class ListFinancialAccountsTool : IAiTool
{
    private readonly ISender _sender;

    public ListFinancialAccountsTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "list_financial_accounts";

    public string Description =>
        "Lista as contas financeiras do usuário (corrente, poupança, dinheiro etc.) com saldo atual. "
        + "Use para perguntas sobre quanto há nas contas ou quais contas existem.";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            includeArchived = new { type = "boolean", description = "Incluir contas arquivadas. Padrão: false." }
        }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var includeArchived = AiToolArguments.GetBool(arguments, "includeArchived");

        var accounts = await _sender.Send(new ListFinancialAccountsQuery(includeArchived), cancellationToken);

        var payload = new
        {
            accounts = accounts.Select(account => new
            {
                account.Id,
                account.Name,
                type = account.Type.ToString(),
                account.CurrentBalance,
                account.IsActive
            }),
            totalBalance = accounts.Where(account => account.IsActive).Sum(account => account.CurrentBalance)
        };

        var sources = accounts
            .Select(account => new AiSource("account", account.Id.ToString(), account.Name))
            .ToList();

        return AiToolResult.Success(AiToolJson.Serialize(payload), sources);
    }
}
