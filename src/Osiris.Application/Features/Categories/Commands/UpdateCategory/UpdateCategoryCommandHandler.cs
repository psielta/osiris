using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categories,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _categories = categories;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.Type is null)
        {
            return Result.Failure(new ResultError("Category type is required.", nameof(request.Type)));
        }

        var tenantId = _currentUser.TenantId;
        var category = await _categories.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(new ResultError("Category was not found."));
        }

        var normalizedName = FinancialCategory.NormalizeName(request.Name);
        var exists = await _categories.ExistsAsync(
            tenantId,
            normalizedName,
            request.Type.Value,
            request.Id,
            cancellationToken);

        if (exists)
        {
            return Result.Failure(new ResultError(
                "A category with this name and type already exists.",
                nameof(request.Name)));
        }

        category.Update(request.Name, request.Type.Value, request.Color, _dateTimeProvider.UtcNow);
        await _categories.UpdateAsync(category, cancellationToken);

        return Result.Success();
    }
}
