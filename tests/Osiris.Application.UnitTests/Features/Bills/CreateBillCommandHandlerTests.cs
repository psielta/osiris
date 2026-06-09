using Osiris.Application.Features.Bills.Commands.CreateBill;
using Osiris.Application.UnitTests.Features.Bills.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Bills;

public sealed class CreateBillCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeBillRepository _bills = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeFinancialAccountRepository _accounts = new();

    private readonly FinancialCategory _expenseCategory;

    public CreateBillCommandHandlerTests()
    {
        _expenseCategory = new FinancialCategory(_tenantId, "Moradia", CategoryType.Expense);
        _categories.Add(_expenseCategory);
    }

    private CreateBillCommandHandler CreateHandler()
    {
        return new CreateBillCommandHandler(_bills, _categories, _accounts, new FakeCurrentUser(_tenantId));
    }

    private CreateBillCommand Command(Guid? categoryId = null, Guid? accountId = null)
    {
        return new CreateBillCommand(
            "Aluguel",
            1200m,
            new DateOnly(2026, 6, 10),
            categoryId ?? _expenseCategory.Id,
            accountId,
            Notes: null);
    }

    [Fact]
    public async Task Handle_ShouldCreateBillForCurrentTenant()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var bill = Assert.Single(_bills.Bills);
        Assert.Equal(_tenantId, bill.TenantId);
        Assert.Equal("Aluguel", bill.Description);
        Assert.Equal(1200m, bill.Amount);
        Assert.Null(bill.PaidAt);
    }

    [Fact]
    public async Task Handle_WithPlannedAccount_ShouldStoreAccount()
    {
        var account = new FinancialAccount(_tenantId, "Banco", FinancialAccountType.CheckingAccount, 500m);
        _accounts.Add(account);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(accountId: account.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(account.Id, Assert.Single(_bills.Bills).PaymentAccountId);
        Assert.Equal(500m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenCategoryFromAnotherTenant_ShouldReject()
    {
        var foreignCategory = new FinancialCategory(Guid.NewGuid(), "Moradia", CategoryType.Expense);
        _categories.Add(foreignCategory);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(categoryId: foreignCategory.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_bills.Bills);
    }

    [Fact]
    public async Task Handle_WhenCategoryIsIncome_ShouldReject()
    {
        var incomeCategory = new FinancialCategory(_tenantId, "Salário", CategoryType.Income);
        _categories.Add(incomeCategory);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(categoryId: incomeCategory.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_bills.Bills);
    }

    [Fact]
    public async Task Handle_WhenCategoryArchived_ShouldReject()
    {
        _expenseCategory.Archive(DateTime.UtcNow);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_bills.Bills);
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
        Assert.Empty(_bills.Bills);
    }

    [Fact]
    public async Task Handle_WhenAccountArchived_ShouldReject()
    {
        var account = new FinancialAccount(_tenantId, "Banco", FinancialAccountType.CheckingAccount, 500m);
        account.Archive(DateTime.UtcNow);
        _accounts.Add(account);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(accountId: account.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_bills.Bills);
    }
}
