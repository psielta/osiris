using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.CreditCards.Commands.CreateCreditCard;

public sealed record CreateCreditCardCommand(
    string Name,
    decimal? Limit,
    int? ClosingDay,
    int? DueDay,
    Guid? PaymentAccountId) : IRequest<Result<Guid>>;
