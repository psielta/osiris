using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;

public sealed record CreateManualMovementCommand(
    Guid AccountId,
    FinancialAccountMovementType? Type,
    decimal? Amount,
    DateOnly? OccurredOn,
    string Description,
    Guid? CategoryId,
    string? Notes) : IRequest<Result<Guid>>;
