using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.Bills.Queries.GetBillDetails;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>Write tool: proposes marking a bill (conta a pagar) as paid. Persists a proposal only.</summary>
public sealed class ProposeBillPaymentTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeBillPaymentTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_bill_payment";

    public string Description =>
        "Cria uma PROPOSTA para marcar uma conta a pagar como paga. NÃO registra: o usuário confirma depois. "
        + "Informe billId; paidAt (ISO) e paymentAccountId são opcionais.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            billId = new { type = "string", description = "Id da conta a pagar (GUID)." },
            paidAt = new { type = "string", description = "Data do pagamento (ISO yyyy-MM-dd). Opcional; padrão hoje." },
            paymentAccountId = new { type = "string", description = "Conta de onde sai o dinheiro (GUID). Opcional." }
        },
        required = new[] { "billId" }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var billId = AiToolArguments.GetGuid(arguments, "billId");
        if (billId is null)
        {
            return AiToolResult.Failure("Informe um billId válido.");
        }

        var bill = await _sender.Send(new GetBillDetailsQuery(billId.Value), cancellationToken);
        if (bill is null)
        {
            return AiToolResult.Failure("Conta a pagar não encontrada.");
        }

        if (bill.PaidAt is not null)
        {
            return AiToolResult.Failure("Esta conta já está paga.");
        }

        var paidAt = AiToolArguments.GetDate(arguments, "paidAt") ?? context.Today;
        var paymentAccountId = AiToolArguments.GetGuid(arguments, "paymentAccountId");

        var payload = new BillPaymentPayload(bill.Id, paidAt, paymentAccountId);
        var stateHash = ProposalState.BillHash(bill.PaidAt, bill.Amount);

        var display = $"Marcar a conta \"{bill.Description}\" ({ProposalFormatting.Money(bill.Amount)}) como paga";
        var impact = paymentAccountId is not null || bill.PaymentAccountId is not null
            ? "Registra o pagamento e debita a conta de pagamento."
            : "Registra o pagamento da conta.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.BillPayment, payload, display, impact, stateHash, cancellationToken);
    }
}
