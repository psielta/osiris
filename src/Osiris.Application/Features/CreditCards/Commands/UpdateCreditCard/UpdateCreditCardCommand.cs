using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.CreditCards.Commands.UpdateCreditCard;

public sealed record UpdateCreditCardCommand(
    Guid Id,
    string Name,
    decimal? Limit,
    int? ClosingDay,
    int? DueDay,
    Guid? PaymentAccountId) : IRequest<Result>;
