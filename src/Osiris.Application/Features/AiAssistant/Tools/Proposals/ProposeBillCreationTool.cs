using System.Globalization;
using System.Text.Json;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>Write tool: proposes creating an off-card payable (conta a pagar). Persists a proposal only.</summary>
public sealed class ProposeBillCreationTool : IAiTool
{
    private readonly IAiActionProposalFactory _factory;

    public ProposeBillCreationTool(IAiActionProposalFactory factory)
    {
        _factory = factory;
    }

    public string Name => "propose_bill_creation";

    public string Description =>
        "Cria uma PROPOSTA de conta a pagar (obrigação fora do cartão). NÃO registra: o usuário confirma "
        + "depois. Informe description, amount, dueDate (ISO) e categoryId de despesa; paymentAccountId e notes são opcionais.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            description = new { type = "string", description = "Descrição da conta a pagar." },
            amount = new { type = "number", description = "Valor positivo." },
            dueDate = new { type = "string", description = "Vencimento (ISO yyyy-MM-dd)." },
            categoryId = new { type = "string", description = "Categoria de despesa (GUID)." },
            paymentAccountId = new { type = "string", description = "Conta de pagamento planejada (GUID). Opcional." },
            notes = new { type = "string", description = "Observações. Opcional." }
        },
        required = new[] { "description", "amount", "dueDate", "categoryId" }
    };

    public Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var description = AiToolArguments.GetString(arguments, "description");
        if (string.IsNullOrWhiteSpace(description))
        {
            return Task.FromResult(AiToolResult.Failure("Informe a description da conta."));
        }

        var amount = AiToolArguments.GetDecimal(arguments, "amount");
        if (amount is null or <= 0m)
        {
            return Task.FromResult(AiToolResult.Failure("Informe um amount maior que zero."));
        }

        var dueDate = AiToolArguments.GetDate(arguments, "dueDate");
        if (dueDate is null)
        {
            return Task.FromResult(AiToolResult.Failure("Informe a dueDate (ISO yyyy-MM-dd)."));
        }

        var categoryId = AiToolArguments.GetGuid(arguments, "categoryId");
        if (categoryId is null)
        {
            return Task.FromResult(AiToolResult.Failure("Informe um categoryId válido (categoria de despesa)."));
        }

        var payload = new BillCreationPayload(
            description.Trim(),
            amount.Value,
            dueDate.Value,
            categoryId.Value,
            AiToolArguments.GetGuid(arguments, "paymentAccountId"),
            AiToolArguments.GetString(arguments, "notes"));

        var display = $"Criar conta a pagar \"{description.Trim()}\" de {ProposalFormatting.Money(amount.Value)} "
            + $"(vence {dueDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)})";
        var impact = "Será criada uma conta a pagar. A categoria precisa ser de despesa e estar ativa na confirmação.";

        return WriteProposal.PersistAsync(_factory, context, AiActionTypes.BillCreation, payload, display, impact, cancellationToken);
    }
}
