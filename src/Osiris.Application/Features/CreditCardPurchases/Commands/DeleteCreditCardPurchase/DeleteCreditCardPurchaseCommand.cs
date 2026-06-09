using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.CreditCardPurchases.Commands.DeleteCreditCardPurchase;

public sealed record DeleteCreditCardPurchaseCommand(Guid Id) : IRequest<Result>;
