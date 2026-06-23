using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Application.Features.CreditCardStatements.Queries.ListAllCreditCardStatements;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>
/// Aggregates the statements (faturas) of every card in a single call: each statement with its card
/// name, totals, open balance, due date and status, plus the grand total still owed. This exists so
/// broad questions ("quanto devo de faturas", "próximas faturas") are answered in one tool call instead
/// of fanning out <c>get_card_statement</c> per card and exhausting the turn's tool budget.
/// </summary>
public sealed class GetStatementsOverviewTool : IAiTool
{
    private const int MaxStatements = 60;

    private readonly ISender _sender;

    public GetStatementsOverviewTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "get_statements_overview";

    public string Description =>
        "Lista as faturas de TODOS os cartões de uma vez (nome do cartão, competência, vencimento, status, "
        + "total e saldo em aberto) e devolve o total ainda devido. Use para perguntas gerais sobre faturas, "
        + "como \"quanto devo de faturas\", \"valor das próximas faturas\" ou \"faturas em aberto\". "
        + "Por padrão traz só as faturas com saldo em aberto (onlyOpen=true).";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            onlyOpen = new { type = "boolean", description = "Trazer apenas faturas com saldo em aberto. Padrão: true." },
            from = new { type = "string", description = "Início do período por vencimento (ISO yyyy-MM-dd). Opcional." },
            to = new { type = "string", description = "Fim do período por vencimento (ISO yyyy-MM-dd). Opcional." }
        }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var onlyOpen = AiToolArguments.GetBool(arguments, "onlyOpen", fallback: true);
        var from = AiToolArguments.GetDate(arguments, "from");
        var to = AiToolArguments.GetDate(arguments, "to");

        var statements = await _sender.Send(new ListAllCreditCardStatementsQuery(from, to), cancellationToken);

        var selected = statements
            .Where(statement => !onlyOpen || statement.OpenBalance > 0m)
            .OrderBy(statement => statement.DueDate)
            .ThenBy(statement => statement.CreditCardName)
            .Take(MaxStatements)
            .ToList();

        var payload = new
        {
            onlyOpen,
            period = new { from, to },
            totalOpenBalance = selected.Sum(statement => statement.OpenBalance),
            statementCount = selected.Count,
            statements = selected.Select(statement => new
            {
                statement.Id,
                creditCard = statement.CreditCardName,
                statement.ReferenceMonth,
                statement.ReferenceYear,
                statement.DueDate,
                statement.ClosingDate,
                status = statement.Status.ToString(),
                statement.TotalAmount,
                statement.PaidAmount,
                statement.OpenBalance
            })
        };

        var sources = selected
            .Select(statement => new AiSource(
                "creditCardStatement",
                statement.Id.ToString(),
                $"Fatura {statement.CreditCardName} {statement.ReferenceMonth:D2}/{statement.ReferenceYear}"))
            .ToList();

        return AiToolResult.Success(AiToolJson.Serialize(payload), sources);
    }
}
