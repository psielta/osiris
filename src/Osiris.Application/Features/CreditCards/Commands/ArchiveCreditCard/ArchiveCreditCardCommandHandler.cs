using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.CreditCards.Commands.ArchiveCreditCard;

public sealed class ArchiveCreditCardCommandHandler : IRequestHandler<ArchiveCreditCardCommand, Result>
{
    private readonly ICreditCardRepository _creditCards;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveCreditCardCommandHandler(
        ICreditCardRepository creditCards,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _creditCards = creditCards;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveCreditCardCommand request, CancellationToken cancellationToken)
    {
        var creditCard = await _creditCards.GetByIdAsync(_currentUser.TenantId, request.Id, cancellationToken);
        if (creditCard is null)
        {
            return Result.Failure(new ResultError("Cartão não encontrado.", Code: ResultErrorCodes.NotFound));
        }

        creditCard.Archive(_dateTimeProvider.UtcNow);
        await _creditCards.UpdateAsync(creditCard, cancellationToken);

        return Result.Success();
    }
}
