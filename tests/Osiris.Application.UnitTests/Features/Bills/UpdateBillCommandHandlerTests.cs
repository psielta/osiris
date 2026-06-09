using Osiris.Application.Common.Models;
using Osiris.Application.Features.Bills.Commands.UpdateBill;
using Osiris.Application.UnitTests.Features.Bills.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Bills;

public sealed class UpdateBillCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeBillRepository _bills = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeFinancialAccountRepository _accounts = new();

    private readonly FinancialCategory _expenseCategory;
    private readonly Bill _bill;

    public UpdateBillCommandHandlerTests()
    {
        _expenseCategory = new FinancialCategory(_tenantId, "Moradia", CategoryType.Expense);
        _categories.Add(_expenseCategory);

        _bill = new Bill(_tenantId, _expenseCategory.Id, "Aluguel", 1200m, new DateOnly(2026, 6, 10));
        _bills.Add(_bill);
    }

    private UpdateBillCommandHandler CreateHandler()
    {
        return new UpdateBillCommandHandler(
            _bills,
            _categories,
            _accounts,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider());
    }

    private UpdateBillCommand Command(
        Guid? id = null,
        decimal amount = 1300m,
        Guid? categoryId = null,
        Guid? accountId = null)
    {
        return new UpdateBillCommand(
            id ?? _bill.Id,
            "Aluguel reajustado",
            amount,
            new DateOnly(2026, 6, 15),
            categoryId ?? _expenseCategory.Id,
            accountId,
            "Reajuste anual");
    }

    [Fact]
    public async Task Handle_ShouldUpdateFields()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Aluguel reajustado", _bill.Description);
        Assert.Equal(1300m, _bill.Amount);
        Assert.Equal(new DateOnly(2026, 6, 15), _bill.DueDate);
        Assert.Equal("Reajuste anual", _bill.Notes);
        Assert.NotNull(_bill.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenBillNotFound_ShouldReturnNotFound()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(Command(id: Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_WhenBillFromAnotherTenant_ShouldReturnNotFound()
    {
        var foreignBill = new Bill(Guid.NewGuid(), Guid.NewGuid(), "Internet", 100m, new DateOnly(2026, 6, 5));
        _bills.Add(foreignBill);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(id: foreignBill.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
        Assert.Equal("Internet", foreignBill.Description);
    }

    [Fact]
    public async Task Handle_WhenCategoryFromAnotherTenant_ShouldReject()
    {
        var foreignCategory = new FinancialCategory(Guid.NewGuid(), "Moradia", CategoryType.Expense);
        _categories.Add(foreignCategory);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(categoryId: foreignCategory.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Aluguel", _bill.Description);
    }

    [Fact]
    public async Task Handle_WhenCategoryIsIncome_ShouldReject()
    {
        var incomeCategory = new FinancialCategory(_tenantId, "Salário", CategoryType.Income);
        _categories.Add(incomeCategory);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(categoryId: incomeCategory.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_WhenPaidAndAmountChanges_ShouldReject()
    {
        _bill.MarkAsPaid(new DateOnly(2026, 6, 8), null, DateTime.UtcNow);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 999m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(1200m, _bill.Amount);
    }

    [Fact]
    public async Task Handle_WhenPaidAndPaymentAccountChanges_ShouldReject()
    {
        var account = new FinancialAccount(_tenantId, "Banco", FinancialAccountType.CheckingAccount, 500m);
        _accounts.Add(account);
        _bill.MarkAsPaid(new DateOnly(2026, 6, 8), null, DateTime.UtcNow);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 1200m, accountId: account.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(_bill.PaymentAccountId);
    }

    [Fact]
    public async Task Handle_WhenPaidAndMoneyFieldsUnchanged_ShouldAllowEditingOtherFields()
    {
        _bill.MarkAsPaid(new DateOnly(2026, 6, 8), null, DateTime.UtcNow);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 1200m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Aluguel reajustado", _bill.Description);
    }

    [Fact]
    public async Task Handle_WhenAccountFromAnotherTenant_ShouldReject()
    {
        var foreignAccount = new FinancialAccount(
            Guid.NewGuid(),
            "Banco Alheio",
            FinancialAccountType.CheckingAccount,
            500m);
        _accounts.Add(foreignAccount);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(accountId: foreignAccount.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}
