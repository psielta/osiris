using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Bills.Commands.CreateBill;

public sealed class CreateBillCommandHandler : IRequestHandler<CreateBillCommand, Result<Guid>>
{
    private readonly IBillRepository _bills;
    private readonly ICategoryRepository _categories;
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;

    public CreateBillCommandHandler(
        IBillRepository bills,
        ICategoryRepository categories,
        IFinancialAccountRepository accounts,
        ICurrentUser currentUser)
    {
        _bills = bills;
        _categories = categories;
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateBillCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe o valor da conta.", nameof(request.Amount)));
        }

        if (request.DueDate is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe a data de vencimento.", nameof(request.DueDate)));
        }

        if (request.CategoryId is null)
        {
            return Result<Guid>.Failure(new ResultError("Selecione a categoria da conta.", nameof(request.CategoryId)));
        }

        var tenantId = _currentUser.TenantId;

        var categoryError = await ValidateCategoryAsync(tenantId, request.CategoryId.Value, cancellationToken);
        if (categoryError is not null)
        {
            return Result<Guid>.Failure(categoryError);
        }

        if (request.PaymentAccountId is not null)
        {
            var accountError = await ValidateAccountAsync(tenantId, request.PaymentAccountId.Value, cancellationToken);
            if (accountError is not null)
            {
                return Result<Guid>.Failure(accountError);
            }
        }

        var bill = new Bill(
            tenantId,
            request.CategoryId.Value,
            request.Description,
            request.Amount.Value,
            request.DueDate.Value,
            request.PaymentAccountId,
            request.Notes);

        await _bills.AddAsync(bill, cancellationToken);

        return Result<Guid>.Success(bill.Id);
    }

    private async Task<ResultError?> ValidateCategoryAsync(
        Guid tenantId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(tenantId, categoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return new ResultError("Categoria não encontrada ou arquivada.", nameof(CreateBillCommand.CategoryId));
        }

        if (category.Type != CategoryType.Expense)
        {
            return new ResultError(
                "A categoria da conta deve ser uma categoria de despesa.",
                nameof(CreateBillCommand.CategoryId));
        }

        return null;
    }

    private async Task<ResultError?> ValidateAccountAsync(
        Guid tenantId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(tenantId, accountId, cancellationToken);
        if (account is null)
        {
            return new ResultError("Conta não encontrada.", nameof(CreateBillCommand.PaymentAccountId));
        }

        if (!account.IsActive)
        {
            return new ResultError("A conta está arquivada.", nameof(CreateBillCommand.PaymentAccountId));
        }

        return null;
    }
}
