using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Lists the tenant's categories, optionally filtered by type (income/expense).</summary>
public sealed class ListCategoriesTool : IAiTool
{
    private readonly ISender _sender;

    public ListCategoriesTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "list_categories";

    public string Description => "Lista as categorias do usuário. Pode filtrar por tipo: income (receita) ou expense (despesa).";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            type = new { type = "string", description = "Filtra por tipo: income ou expense. Opcional." },
            includeArchived = new { type = "boolean", description = "Incluir categorias arquivadas. Padrão: false." }
        }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var includeArchived = AiToolArguments.GetBool(arguments, "includeArchived");
        var type = AiToolArguments.GetString(arguments, "type");

        var categories = await _sender.Send(new ListCategoriesQuery(includeArchived), cancellationToken);

        var filtered = categories
            .Where(category => string.IsNullOrWhiteSpace(type)
                || string.Equals(category.Type.ToString(), type, StringComparison.OrdinalIgnoreCase))
            .Select(category => new
            {
                category.Id,
                category.Name,
                type = category.Type.ToString(),
                category.Color,
                category.IsActive
            });

        return AiToolResult.Success(AiToolJson.Serialize(new { categories = filtered }));
    }
}
