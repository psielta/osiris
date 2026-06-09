using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.CreditCardPurchases.Commands.CreateCreditCardPurchase;

public sealed record CreateCreditCardPurchaseCommand(
    Guid CreditCardId,
    Guid? CategoryId,
    string Description,
    decimal? TotalAmount,
    DateOnly? PurchaseDate,
    int? Installments,
    string? Notes) : IRequest<Result<Guid>>;
