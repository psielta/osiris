using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Bills.Commands.UpdateBill;

public sealed record UpdateBillCommand(
    Guid Id,
    string Description,
    decimal? Amount,
    DateOnly? DueDate,
    Guid? CategoryId,
    Guid? PaymentAccountId,
    string? Notes) : IRequest<Result>;
