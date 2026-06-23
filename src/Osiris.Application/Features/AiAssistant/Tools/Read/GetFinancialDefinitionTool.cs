using System.Globalization;
using System.Text.Json;
using Osiris.Application.Common.AI;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>
/// Returns the official Osiris definition for a financial term from a controlled, in-repo glossary.
/// Keeps the agent aligned with the domain semantics (purchase vs statement vs bill, cash vs expense)
/// without inventing meanings.
/// </summary>
public sealed class GetFinancialDefinitionTool : IAiTool
{
    private static readonly IReadOnlyDictionary<string, string> Definitions = new Dictionary<string, string>
    {
        ["compra no cartao"] =
            "Compra no cartão é a despesa categorizada, contada na data da compra. É o que aparece nos gastos por categoria.",
        ["fatura"] =
            "Fatura agrupa as compras de um ciclo do cartão em uma dívida a pagar. Ela não é uma nova despesa: apenas reúne compras já contadas.",
        ["pagamento de fatura"] =
            "Pagamento de fatura é uma saída de caixa que liquida a dívida da fatura. Não conta como uma segunda despesa.",
        ["conta a pagar"] =
            "Conta a pagar é uma obrigação fora do cartão (aluguel, contas de consumo, assinaturas cobradas diretamente).",
        ["fluxo de caixa"] =
            "Fluxo de caixa é a visão de entradas e saídas de dinheiro das contas em um período, incluindo pagamentos de fatura como saída.",
        ["despesa"] =
            "Despesa é o gasto categorizado (compras no cartão, contas a pagar e despesas diretas). É visão diferente do fluxo de caixa.",
        ["saldo projetado"] =
            "Saldo projetado é o saldo das contas considerando as obrigações em aberto previstas para o período.",
        ["parcelamento"] =
            "Parcelamento divide uma compra no cartão em parcelas; cada parcela entra na fatura do seu mês.",
    };

    public string Name => "get_financial_definition";

    public string Description =>
        "Retorna a definição oficial do Osiris para um termo financeiro (ex.: compra no cartão, fatura, "
        + "pagamento de fatura, conta a pagar, fluxo de caixa, despesa). Use para não confundir os conceitos.";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            term = new { type = "string", description = "Termo a definir (ex.: 'fatura', 'fluxo de caixa')." }
        },
        required = new[] { "term" }
    };

    public Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var term = AiToolArguments.GetString(arguments, "term");
        if (string.IsNullOrWhiteSpace(term))
        {
            return Task.FromResult(AiToolResult.Failure("Informe um termo para definir."));
        }

        var key = Normalize(term);
        var match = Definitions.FirstOrDefault(entry => key.Contains(entry.Key) || entry.Key.Contains(key));

        if (match.Value is null)
        {
            var notFound = new { term, found = false, definition = (string?)null, knownTerms = Definitions.Keys.ToArray() };
            return Task.FromResult(AiToolResult.Success(AiToolJson.Serialize(notFound)));
        }

        var payload = new { term, found = true, definition = match.Value };
        return Task.FromResult(AiToolResult.Success(AiToolJson.Serialize(payload)));
    }

    private static string Normalize(string value)
    {
        var lowered = value.Trim().ToLower(CultureInfo.InvariantCulture);
        var builder = new System.Text.StringBuilder(lowered.Length);
        foreach (var character in lowered.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
