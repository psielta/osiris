using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Categories.Commands.ArchiveCategory;

public sealed class ArchiveCategoryCommandHandler : IRequestHandler<ArchiveCategoryCommand, Result>
{
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveCategoryCommandHandler(
        ICategoryRepository categories,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _categories = categories;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(_currentUser.TenantId, request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(new ResultError("Category was not found."));
        }

        category.Archive(_dateTimeProvider.UtcNow);
        await _categories.UpdateAsync(category, cancellationToken);

        return Result.Success();
    }
}
