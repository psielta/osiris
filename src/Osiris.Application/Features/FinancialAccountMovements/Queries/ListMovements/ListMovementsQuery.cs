using MediatR;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;

namespace Osiris.Application.Features.FinancialAccountMovements.Queries.ListMovements;

public sealed record ListMovementsQuery(Guid AccountId)
    : IRequest<IReadOnlyCollection<MovementListItemDto>>;
