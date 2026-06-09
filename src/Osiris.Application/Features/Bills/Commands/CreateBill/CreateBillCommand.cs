using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Bills.Commands.CreateBill;

public sealed record CreateBillCommand(
    string Description,
    decimal? Amount,
    DateOnly? DueDate,
    Guid? CategoryId,
    Guid? PaymentAccountId,
    string? Notes) : IRequest<Result<Guid>>;
