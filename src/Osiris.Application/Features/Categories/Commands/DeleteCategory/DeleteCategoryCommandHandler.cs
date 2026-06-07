using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;

    public DeleteCategoryCommandHandler(ICategoryRepository categories, ICurrentUser currentUser)
    {
        _categories = categories;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(_currentUser.TenantId, request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(new ResultError("Category was not found."));
        }

        await _categories.DeleteAsync(category, cancellationToken);

        return Result.Success();
    }
}
