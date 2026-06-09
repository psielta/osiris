using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;

public sealed record MarkBillAsPaidCommand(
    Guid Id,
    DateOnly? PaidAt,
    Guid? PaymentAccountId) : IRequest<Result>;
