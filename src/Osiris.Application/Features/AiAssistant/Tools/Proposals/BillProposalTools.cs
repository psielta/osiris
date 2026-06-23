using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.Bills.Queries.GetBillDetails;
using Osiris.Application.Features.Bills.Queries.GetBillForEdit;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>Write tool: proposes editing an existing bill (conta a pagar). Persists a proposal only.</summary>
public sealed class ProposeBillUpdateTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeBillUpdateTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_bill_update";

    public string Description =>
        "Cria uma PROPOSTA de edição de uma conta a pagar (descrição, valor, vencimento, categoria, conta de "
        + "pagamento, observações). NÃO registra: o usuário confirma depois. Informe billId (de list_bills); os "
        + "demais campos são opcionais (o que não for informado permanece igual).";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            billId = new { type = "string", description = "Id da conta a pagar (GUID)." },
            description = new { type = "string", description = "Nova descrição. Opcional." },
            amount = new { type = "number", description = "Novo valor. Opcional." },
            dueDate = new { type = "string", description = "Novo vencimento (ISO yyyy-MM-dd). Opcional." },
            categoryId = new { type = "string", description = "Nova categoria de despesa (GUID). Opcional." },
            paymentAccountId = new { type = "string", description = "Conta de pagamento (GUID). Opcional." },
            notes = new { type = "string", description = "Observações. Opcional." }
        },
        required = new[] { "billId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var billId = AiToolArguments.GetGuid(arguments, "billId");
        if (billId is null)
        {
            return AiToolResult.Failure("Informe um billId válido.");
        }

        var current = await _sender.Send(new GetBillForEditQuery(billId.Value), cancellationToken);
        if (current is null)
        {
            return AiToolResult.Failure("Conta a pagar não encontrada.");
        }

        var description = AiToolArguments.GetString(arguments, "description")?.Trim();
        var finalDescription = string.IsNullOrWhiteSpace(description) ? current.Description : description;
        var finalAmount = AiToolArguments.GetDecimal(arguments, "amount") ?? current.Amount;
        var finalDueDate = AiToolArguments.GetDate(arguments, "dueDate") ?? current.DueDate;
        var finalCategoryId = AiToolArguments.GetGuid(arguments, "categoryId") ?? current.CategoryId;
        var finalPaymentAccountId = AiToolArguments.GetGuid(arguments, "paymentAccountId") ?? current.PaymentAccountId;
        var notes = AiToolArguments.GetString(arguments, "notes");
        var finalNotes = notes ?? current.Notes;

        if (finalAmount <= 0m)
        {
            return AiToolResult.Failure("O valor deve ser maior que zero.");
        }

        var payload = new BillUpdatePayload(
            current.Id, finalDescription, finalAmount, finalDueDate, finalCategoryId, finalPaymentAccountId, finalNotes);
        var stateHash = ProposalState.BillEditHash(current.Description, current.Amount, current.DueDate, current.CategoryId, current.IsPaid);
        var display = $"Editar a conta a pagar \"{current.Description}\" (valor {ProposalFormatting.Money(finalAmount)}, vence {finalDueDate:dd/MM/yyyy})";
        var impact = "Atualiza o cadastro da conta a pagar.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.BillUpdate, payload, display, impact, stateHash, cancellationToken);
    }
}

/// <summary>Write tool: proposes permanently deleting a bill (conta a pagar). Persists a proposal only.</summary>
public sealed class ProposeBillDeletionTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeBillDeletionTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_bill_deletion";

    public string Description =>
        "Cria uma PROPOSTA de EXCLUSÃO de uma conta a pagar. Ação irreversível. NÃO registra: o usuário confirma "
        + "depois. Informe billId (de list_bills).";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new { billId = new { type = "string", description = "Id da conta a pagar (GUID)." } },
        required = new[] { "billId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var billId = AiToolArguments.GetGuid(arguments, "billId");
        if (billId is null)
        {
            return AiToolResult.Failure("Informe um billId válido.");
        }

        var current = await _sender.Send(new GetBillForEditQuery(billId.Value), cancellationToken);
        if (current is null)
        {
            return AiToolResult.Failure("Conta a pagar não encontrada.");
        }

        var payload = new BillRefPayload(current.Id);
        var stateHash = ProposalState.BillEditHash(current.Description, current.Amount, current.DueDate, current.CategoryId, current.IsPaid);
        var display = $"Excluir a conta a pagar \"{current.Description}\" ({ProposalFormatting.Money(current.Amount)})";
        var impact = "Ação irreversível: remove a conta a pagar do sistema.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.BillDeletion, payload, display, impact, stateHash, cancellationToken);
    }
}

/// <summary>Write tool: proposes reverting a paid bill back to pending (estorno). Persists a proposal only.</summary>
public sealed class ProposeBillUnpayTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeBillUnpayTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_bill_unpay";

    public string Description =>
        "Cria uma PROPOSTA de estorno do pagamento de uma conta a pagar (volta de paga para pendente). NÃO "
        + "registra: o usuário confirma depois. Informe billId.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new { billId = new { type = "string", description = "Id da conta a pagar (GUID)." } },
        required = new[] { "billId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
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

        if (bill.Status != BillStatus.Paid)
        {
            return AiToolResult.Failure("A conta a pagar não está marcada como paga.");
        }

        var payload = new BillRefPayload(bill.Id);
        var stateHash = ProposalState.BillHash(bill.PaidAt, bill.Amount);
        var display = $"Estornar o pagamento da conta \"{bill.Description}\" (volta para pendente)";
        var impact = "A conta volta a ficar pendente; o lançamento de pagamento é desfeito.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.BillUnpay, payload, display, impact, stateHash, cancellationToken);
    }
}
