using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.CreditCards.Commands.UpdateCreditCard;

public sealed class UpdateCreditCardCommandHandler : IRequestHandler<UpdateCreditCardCommand, Result>
{
    private readonly ICreditCardRepository _creditCards;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCreditCardCommandHandler(
        ICreditCardRepository creditCards,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _creditCards = creditCards;
        _accounts = accounts;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateCreditCardCommand request, CancellationToken cancellationToken)
    {
        if (request.Limit is null)
        {
            return Result.Failure(new ResultError("Informe o limite do cartão.", nameof(request.Limit)));
        }

        if (request.ClosingDay is null)
        {
            return Result.Failure(new ResultError("Informe o dia de fechamento.", nameof(request.ClosingDay)));
        }

        if (request.DueDay is null)
        {
            return Result.Failure(new ResultError("Informe o dia de vencimento.", nameof(request.DueDay)));
        }

        var tenantId = _currentUser.TenantId;
        var creditCard = await _creditCards.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (creditCard is null)
        {
            return Result.Failure(new ResultError("Cartão não encontrado.", Code: ResultErrorCodes.NotFound));
        }

        var normalizedName = CreditCard.NormalizeName(request.Name);
        var exists = await _creditCards.ExistsAsync(tenantId, normalizedName, request.Id, cancellationToken);
        if (exists)
        {
            return Result.Failure(new ResultError("Já existe um cartão com este nome.", nameof(request.Name)));
        }

        if (request.PaymentAccountId is not null)
        {
            var account = await _accounts.GetByIdAsync(tenantId, request.PaymentAccountId.Value, cancellationToken);
            if (account is null)
            {
                return Result.Failure(new ResultError(
                    "Conta de pagamento não encontrada.",
                    nameof(request.PaymentAccountId)));
            }
        }

        creditCard.Update(
            request.Name,
            request.Limit.Value,
            request.ClosingDay.Value,
            request.DueDay.Value,
            request.PaymentAccountId,
            _dateTimeProvider.UtcNow);
        await _creditCards.UpdateAsync(creditCard, cancellationToken);

        return Result.Success();
    }
}
