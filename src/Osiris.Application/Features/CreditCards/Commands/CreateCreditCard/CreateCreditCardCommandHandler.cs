using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.CreditCards.Commands.CreateCreditCard;

public sealed class CreateCreditCardCommandHandler : IRequestHandler<CreateCreditCardCommand, Result<Guid>>
{
    private readonly ICreditCardRepository _creditCards;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;

    public CreateCreditCardCommandHandler(
        ICreditCardRepository creditCards,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser)
    {
        _creditCards = creditCards;
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateCreditCardCommand request, CancellationToken cancellationToken)
    {
        if (request.Limit is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe o limite do cartão.", nameof(request.Limit)));
        }

        if (request.ClosingDay is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe o dia de fechamento.", nameof(request.ClosingDay)));
        }

        if (request.DueDay is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe o dia de vencimento.", nameof(request.DueDay)));
        }

        var tenantId = _currentUser.TenantId;
        var normalizedName = CreditCard.NormalizeName(request.Name);
        var exists = await _creditCards.ExistsAsync(tenantId, normalizedName, excludeId: null, cancellationToken);
        if (exists)
        {
            return Result<Guid>.Failure(new ResultError("Já existe um cartão com este nome.", nameof(request.Name)));
        }

        if (request.PaymentAccountId is not null)
        {
            var account = await _accounts.GetByIdAsync(tenantId, request.PaymentAccountId.Value, cancellationToken);
            if (account is null)
            {
                return Result<Guid>.Failure(new ResultError(
                    "Conta de pagamento não encontrada.",
                    nameof(request.PaymentAccountId)));
            }
        }

        var creditCard = new CreditCard(
            tenantId,
            request.Name,
            request.Limit.Value,
            request.ClosingDay.Value,
            request.DueDay.Value,
            request.PaymentAccountId);
        await _creditCards.AddAsync(creditCard, cancellationToken);

        return Result<Guid>.Success(creditCard.Id);
    }
}
