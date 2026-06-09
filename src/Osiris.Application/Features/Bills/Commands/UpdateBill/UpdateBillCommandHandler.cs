using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Bills.Commands.UpdateBill;

public sealed class UpdateBillCommandHandler : IRequestHandler<UpdateBillCommand, Result>
{
    private readonly IBillRepository _bills;
    private readonly ICategoryRepository _categories;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBillCommandHandler(
        IBillRepository bills,
        ICategoryRepository categories,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _bills = bills;
        _categories = categories;
        _accounts = accounts;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBillCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount is null)
        {
            return Result.Failure(new ResultError("Informe o valor da conta.", nameof(request.Amount)));
        }

        if (request.DueDate is null)
        {
            return Result.Failure(new ResultError("Informe a data de vencimento.", nameof(request.DueDate)));
        }

        if (request.CategoryId is null)
        {
            return Result.Failure(new ResultError("Selecione a categoria da conta.", nameof(request.CategoryId)));
        }

        var tenantId = _currentUser.TenantId;

        var bill = await _bills.GetByIdAsync(tenantId, request.Id, cancellationToken);
        if (bill is null)
        {
            return Result.Failure(new ResultError("Conta não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        // A paid bill may have an account movement of the original amount; changing money fields
        // would desync the cash trail. The user must reopen the bill first.
        if (bill.IsPaid && bill.Amount != request.Amount.Value)
        {
            return Result.Failure(new ResultError(
                "Não é possível alterar o valor de uma conta paga. Marque a conta como pendente primeiro.",
                nameof(request.Amount)));
        }

        if (bill.IsPaid && bill.PaymentAccountId != request.PaymentAccountId)
        {
            return Result.Failure(new ResultError(
                "Não é possível alterar a conta de pagamento de uma conta paga. Marque a conta como pendente primeiro.",
                nameof(request.PaymentAccountId)));
        }

        var category = await _categories.GetByIdAsync(tenantId, request.CategoryId.Value, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return Result.Failure(new ResultError(
                "Categoria não encontrada ou arquivada.",
                nameof(request.CategoryId)));
        }

        if (category.Type != CategoryType.Expense)
        {
            return Result.Failure(new ResultError(
                "A categoria da conta deve ser uma categoria de despesa.",
                nameof(request.CategoryId)));
        }

        if (request.PaymentAccountId is not null)
        {
            var account = await _accounts.GetByIdAsync(tenantId, request.PaymentAccountId.Value, cancellationToken);
            if (account is null)
            {
                return Result.Failure(new ResultError("Conta não encontrada.", nameof(request.PaymentAccountId)));
            }

            if (!account.IsActive && bill.PaymentAccountId != account.Id)
            {
                return Result.Failure(new ResultError("A conta está arquivada.", nameof(request.PaymentAccountId)));
            }
        }

        bill.Update(
            request.CategoryId.Value,
            request.Description,
            request.Amount.Value,
            request.DueDate.Value,
            request.PaymentAccountId,
            request.Notes,
            _dateTimeProvider.UtcNow);

        await _bills.UpdateAsync(bill, cancellationToken);

        return Result.Success();
    }
}
