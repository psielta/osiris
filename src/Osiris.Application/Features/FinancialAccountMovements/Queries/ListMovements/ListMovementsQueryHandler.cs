using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Queries.ListMovements;

public sealed class ListMovementsQueryHandler
    : IRequestHandler<ListMovementsQuery, IReadOnlyCollection<MovementListItemDto>>
{
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly ICurrentUser _currentUser;

    public ListMovementsQueryHandler(IFinancialAccountMovementRepository movements, ICurrentUser currentUser)
    {
        _movements = movements;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<MovementListItemDto>> Handle(
        ListMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var movements = await _movements.ListByAccountAsync(
            _currentUser.TenantId,
            request.AccountId,
            cancellationToken);

        return movements
            .Select(movement => new MovementListItemDto(
                movement.Id,
                movement.Type,
                movement.Amount,
                movement.Type.IsInflow(),
                movement.OccurredOn,
                movement.Description,
                movement.CategoryId,
                movement.Notes))
            .ToArray();
    }
}
