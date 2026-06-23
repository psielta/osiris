using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.CreditCardPurchases.Commands.ChangeCreditCardPurchaseCategory;

/// <summary>Reclassifies an existing credit card purchase under a different expense category.</summary>
public sealed record ChangeCreditCardPurchaseCategoryCommand(Guid PurchaseId, Guid CategoryId) : IRequest<Result>;
