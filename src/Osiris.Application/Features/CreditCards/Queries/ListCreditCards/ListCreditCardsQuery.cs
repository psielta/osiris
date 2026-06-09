using MediatR;
using Osiris.Application.Features.CreditCards.DTOs;

namespace Osiris.Application.Features.CreditCards.Queries.ListCreditCards;

public sealed record ListCreditCardsQuery(bool IncludeArchived = true)
    : IRequest<IReadOnlyCollection<CreditCardListItemDto>>;
