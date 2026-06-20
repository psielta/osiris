using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.ImportOfxStatement;

/// <summary>
/// Confirms an OFX import: persists the selected transactions as account movements, skipping any that
/// were already imported (by external id). Amounts are positive; the direction is carried by <c>Type</c>.
/// </summary>
public sealed record ImportOfxStatementCommand(
    Guid AccountId,
    IReadOnlyList<ImportOfxLineInput> Lines) : IRequest<Result<OfxImportResultDto>>;

public sealed record ImportOfxLineInput(
    string ExternalId,
    DateOnly OccurredOn,
    decimal Amount,
    FinancialAccountMovementType Type,
    string Description,
    Guid? CategoryId);
