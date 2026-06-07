using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;

    public CreateCategoryCommandHandler(ICategoryRepository categories, ICurrentUser currentUser)
    {
        _categories = categories;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.Type is null)
        {
            return Result<Guid>.Failure(new ResultError("Selecione o tipo da categoria.", nameof(request.Type)));
        }

        var tenantId = _currentUser.TenantId;
        var normalizedName = FinancialCategory.NormalizeName(request.Name);
        var exists = await _categories.ExistsAsync(
            tenantId,
            normalizedName,
            request.Type.Value,
            excludeId: null,
            cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(new ResultError(
                "Já existe uma categoria com este nome e tipo.",
                nameof(request.Name)));
        }

        var category = new FinancialCategory(tenantId, request.Name, request.Type.Value, request.Color);
        await _categories.AddAsync(category, cancellationToken);

        return Result<Guid>.Success(category.Id);
    }
}
