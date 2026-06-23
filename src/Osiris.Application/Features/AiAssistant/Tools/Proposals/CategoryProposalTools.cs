using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.Categories.DTOs;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>Shared helpers for the category write-proposal tools.</summary>
internal static class CategoryProposalSupport
{
    public static string? ParseType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "income" or "receita" => CategoryType.Income.ToString(),
        "expense" or "despesa" => CategoryType.Expense.ToString(),
        _ => null
    };

    public static string TypeLabel(CategoryType type) => type == CategoryType.Income ? "receita" : "despesa";

    public static async Task<CategoryListItemDto?> FindAsync(ISender sender, Guid id, CancellationToken ct)
    {
        var categories = await sender.Send(new ListCategoriesQuery(IncludeArchived: true), ct);
        return categories.FirstOrDefault(category => category.Id == id);
    }
}

/// <summary>Write tool: proposes creating a category (income or expense). Persists a proposal only.</summary>
public sealed class ProposeCategoryCreationTool : IAiTool
{
    private readonly IAiActionProposalFactory _factory;

    public ProposeCategoryCreationTool(IAiActionProposalFactory factory) => _factory = factory;

    public string Name => "propose_category_creation";

    public string Description =>
        "Cria uma PROPOSTA de nova categoria. NÃO registra: o usuário confirma depois. Informe name e type "
        + "(income = receita, expense = despesa); color (hex, ex. #22c55e) é opcional.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string", description = "Nome da categoria." },
            type = new { type = "string", description = "income (receita) ou expense (despesa)." },
            color = new { type = "string", description = "Cor em hex (ex. #22c55e). Opcional." }
        },
        required = new[] { "name", "type" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var name = AiToolArguments.GetString(arguments, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return AiToolResult.Failure("Informe o nome da categoria.");
        }

        var type = CategoryProposalSupport.ParseType(AiToolArguments.GetString(arguments, "type"));
        if (type is null)
        {
            return AiToolResult.Failure("Informe o tipo: income (receita) ou expense (despesa).");
        }

        var payload = new CategoryCreationPayload(name.Trim(), type, AiToolArguments.GetString(arguments, "color"));
        var label = type == CategoryType.Income.ToString() ? "receita" : "despesa";
        var display = $"Criar categoria \"{name.Trim()}\" ({label})";
        var impact = "Adiciona uma nova categoria para classificar lançamentos.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.CategoryCreation, payload, display, impact, cancellationToken);
    }
}

/// <summary>Write tool: proposes editing a category's name, type or color. Persists a proposal only.</summary>
public sealed class ProposeCategoryUpdateTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeCategoryUpdateTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_category_update";

    public string Description =>
        "Cria uma PROPOSTA de edição de uma categoria (renomear, mudar tipo ou cor). NÃO registra: o usuário "
        + "confirma depois. Informe categoryId (de list_categories); name, type e color são opcionais (o que não "
        + "for informado permanece igual).";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            categoryId = new { type = "string", description = "Id da categoria (GUID)." },
            name = new { type = "string", description = "Novo nome. Opcional." },
            type = new { type = "string", description = "Novo tipo: income ou expense. Opcional." },
            color = new { type = "string", description = "Nova cor em hex. Opcional." }
        },
        required = new[] { "categoryId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var categoryId = AiToolArguments.GetGuid(arguments, "categoryId");
        if (categoryId is null)
        {
            return AiToolResult.Failure("Informe um categoryId válido.");
        }

        var current = await CategoryProposalSupport.FindAsync(_sender, categoryId.Value, cancellationToken);
        if (current is null)
        {
            return AiToolResult.Failure("Categoria não encontrada.");
        }

        var name = AiToolArguments.GetString(arguments, "name")?.Trim();
        var finalName = string.IsNullOrWhiteSpace(name) ? current.Name : name;

        var typeArg = AiToolArguments.GetString(arguments, "type");
        var finalType = current.Type.ToString();
        if (!string.IsNullOrWhiteSpace(typeArg))
        {
            var parsed = CategoryProposalSupport.ParseType(typeArg);
            if (parsed is null)
            {
                return AiToolResult.Failure("Tipo inválido: use income ou expense.");
            }

            finalType = parsed;
        }

        var colorArg = AiToolArguments.GetString(arguments, "color");
        var finalColor = string.IsNullOrWhiteSpace(colorArg) ? current.Color : colorArg;

        var payload = new CategoryUpdatePayload(current.Id, finalName, finalType, finalColor);
        var stateHash = ProposalState.CategoryHash(current.Name, current.Type, current.Color, current.IsActive);
        var display = $"Editar a categoria \"{current.Name}\" (novo nome: \"{finalName}\", tipo: {CategoryProposalSupport.TypeLabel(Enum.Parse<CategoryType>(finalType))})";
        var impact = "Atualiza o cadastro da categoria; lançamentos existentes mantêm o vínculo.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.CategoryUpdate, payload, display, impact, stateHash, cancellationToken);
    }
}

/// <summary>Write tool: proposes archiving (hiding) a category. Persists a proposal only.</summary>
public sealed class ProposeCategoryArchiveTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeCategoryArchiveTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_category_archive";

    public string Description =>
        "Cria uma PROPOSTA de arquivamento de uma categoria (deixa de aparecer para novos lançamentos, sem apagar "
        + "o histórico). NÃO registra: o usuário confirma depois. Informe categoryId.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new { categoryId = new { type = "string", description = "Id da categoria (GUID)." } },
        required = new[] { "categoryId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var categoryId = AiToolArguments.GetGuid(arguments, "categoryId");
        if (categoryId is null)
        {
            return AiToolResult.Failure("Informe um categoryId válido.");
        }

        var current = await CategoryProposalSupport.FindAsync(_sender, categoryId.Value, cancellationToken);
        if (current is null)
        {
            return AiToolResult.Failure("Categoria não encontrada.");
        }

        if (!current.IsActive)
        {
            return AiToolResult.Failure("A categoria já está arquivada.");
        }

        var payload = new CategoryRefPayload(current.Id);
        var stateHash = ProposalState.CategoryHash(current.Name, current.Type, current.Color, current.IsActive);
        var display = $"Arquivar a categoria \"{current.Name}\"";
        var impact = "A categoria deixa de aparecer para novos lançamentos; o histórico é preservado.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.CategoryArchive, payload, display, impact, stateHash, cancellationToken);
    }
}

/// <summary>Write tool: proposes permanently deleting a category. Persists a proposal only.</summary>
public sealed class ProposeCategoryDeletionTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeCategoryDeletionTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_category_deletion";

    public string Description =>
        "Cria uma PROPOSTA de EXCLUSÃO DEFINITIVA de uma categoria. Ação irreversível; só é possível se não houver "
        + "lançamentos usando a categoria (caso contrário, prefira arquivar). NÃO registra: o usuário confirma depois. "
        + "Informe categoryId.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new { categoryId = new { type = "string", description = "Id da categoria (GUID)." } },
        required = new[] { "categoryId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var categoryId = AiToolArguments.GetGuid(arguments, "categoryId");
        if (categoryId is null)
        {
            return AiToolResult.Failure("Informe um categoryId válido.");
        }

        var current = await CategoryProposalSupport.FindAsync(_sender, categoryId.Value, cancellationToken);
        if (current is null)
        {
            return AiToolResult.Failure("Categoria não encontrada.");
        }

        var payload = new CategoryRefPayload(current.Id);
        var stateHash = ProposalState.CategoryHash(current.Name, current.Type, current.Color, current.IsActive);
        var display = $"Excluir definitivamente a categoria \"{current.Name}\"";
        var impact = "Ação irreversível. Se houver lançamentos usando a categoria, a exclusão falha — use arquivar.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.CategoryDeletion, payload, display, impact, stateHash, cancellationToken);
    }
}
