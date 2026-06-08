using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;

public sealed class CreateManualMovementCommandHandler : IRequestHandler<CreateManualMovementCommand, Result<Guid>>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateManualMovementCommandHandler(
        IFinancialAccountRepository accounts,
        IFinancialAccountMovementRepository movements,
        ICategoryRepository categories,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _accounts = accounts;
        _movements = movements;
        _categories = categories;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateManualMovementCommand request, CancellationToken cancellationToken)
    {
        if (request.Type is null)
        {
            return Result<Guid>.Failure(new ResultError("Selecione o tipo do lançamento.", nameof(request.Type)));
        }

        if (request.Type is not (FinancialAccountMovementType.Income or FinancialAccountMovementType.Expense))
        {
            return Result<Guid>.Failure(new ResultError("Selecione um tipo de lançamento válido.", nameof(request.Type)));
        }

        if (request.Amount is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe o valor do lançamento.", nameof(request.Amount)));
        }

        if (request.OccurredOn is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe a data do lançamento.", nameof(request.OccurredOn)));
        }

        var tenantId = _currentUser.TenantId;
        var account = await _accounts.GetByIdAsync(tenantId, request.AccountId, cancellationToken);
        if (account is null)
        {
            return Result<Guid>.Failure(new ResultError("Conta não encontrada."));
        }

        if (!account.IsActive)
        {
            return Result<Guid>.Failure(new ResultError("A conta está arquivada."));
        }

        if (request.CategoryId is not null)
        {
            var category = await _categories.GetByIdAsync(tenantId, request.CategoryId.Value, cancellationToken);
            if (category is null || !category.IsActive)
            {
                return Result<Guid>.Failure(new ResultError(
                    "Categoria não encontrada ou arquivada.",
                    nameof(request.CategoryId)));
            }
        }

        account.ApplyMovement(request.Type.Value, request.Amount.Value, _dateTimeProvider.UtcNow);

        var movement = new FinancialAccountMovement(
            tenantId,
            account.Id,
            request.Type.Value,
            request.Amount.Value,
            request.OccurredOn.Value,
            request.Description,
            request.CategoryId,
            relatedEntityType: null,
            relatedEntityId: null,
            request.Notes);

        await _movements.AddAsync(movement, account, cancellationToken);

        return Result<Guid>.Success(movement.Id);
    }
}
