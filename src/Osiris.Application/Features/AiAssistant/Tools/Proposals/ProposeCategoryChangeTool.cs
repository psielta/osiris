using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>
/// Write tool: proposes reclassifying a credit card purchase under a different expense category.
/// Persists a proposal only; the reclassification runs later, on confirmation.
/// </summary>
public sealed class ProposeCategoryChangeTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeCategoryChangeTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_category_change";

    public string Description =>
        "Cria uma PROPOSTA de mudança de categoria de uma compra no cartão (reclassifica a despesa). NÃO "
        + "registra: o usuário confirma depois. Informe creditCardPurchaseId (de search_card_purchases) e o "
        + "categoryId de despesa (de list_categories). Não altera valores, parcelas nem faturas.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            creditCardPurchaseId = new { type = "string", description = "Id da compra no cartão (GUID)." },
            categoryId = new { type = "string", description = "Nova categoria de despesa (GUID)." }
        },
        required = new[] { "creditCardPurchaseId", "categoryId" }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var purchaseId = AiToolArguments.GetGuid(arguments, "creditCardPurchaseId");
        if (purchaseId is null)
        {
            return AiToolResult.Failure("Informe um creditCardPurchaseId válido.");
        }

        var categoryId = AiToolArguments.GetGuid(arguments, "categoryId");
        if (categoryId is null)
        {
            return AiToolResult.Failure("Informe um categoryId válido.");
        }

        var purchase = await _sender.Send(new GetCreditCardPurchaseDetailsQuery(purchaseId.Value), cancellationToken);
        if (purchase is null)
        {
            return AiToolResult.Failure("Compra não encontrada.");
        }

        if (purchase.CategoryId == categoryId.Value)
        {
            return AiToolResult.Failure("A compra já está nessa categoria.");
        }

        var categories = await _sender.Send(new ListCategoriesQuery(IncludeArchived: false), cancellationToken);
        var category = categories.FirstOrDefault(item => item.Id == categoryId.Value);
        if (category is null || !category.IsActive)
        {
            return AiToolResult.Failure("Categoria não encontrada ou arquivada.");
        }

        if (category.Type != CategoryType.Expense)
        {
            return AiToolResult.Failure("A categoria da compra deve ser uma categoria de despesa.");
        }

        var payload = new CategoryChangePayload(purchase.Id, category.Id);
        var stateHash = ProposalState.PurchaseCategoryHash(purchase.CategoryId);

        var currentCategory = string.IsNullOrWhiteSpace(purchase.CategoryName) ? "sem categoria" : purchase.CategoryName;
        var display = $"Mudar a categoria da compra \"{purchase.Description}\" de {currentCategory} para {category.Name}";
        var impact = "Reclassifica a compra nos relatórios de gastos; não altera valores, parcelas nem faturas.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.CategoryChange, payload, display, impact, stateHash, cancellationToken);
    }
}
