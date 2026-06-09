using Osiris.Application.Features.Bills.Queries.ListBills;
using Osiris.Application.UnitTests.Features.Bills.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Bills;

public sealed class ListBillsQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeBillRepository _bills = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeFinancialAccountRepository _accounts = new();

    private readonly FinancialCategory _category;

    public ListBillsQueryHandlerTests()
    {
        _category = new FinancialCategory(_tenantId, "Moradia", CategoryType.Expense, "#FF0000");
        _categories.Add(_category);
    }

    private ListBillsQueryHandler CreateHandler(DateTime? utcNow = null)
    {
        return new ListBillsQueryHandler(
            _bills,
            _categories,
            _accounts,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider
            {
                UtcNow = utcNow ?? new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc)
            });
    }

    [Fact]
    public async Task Handle_ShouldFilterByDueDateMonth()
    {
        _bills.Add(new Bill(_tenantId, _category.Id, "Aluguel junho", 1200m, new DateOnly(2026, 6, 10)));
        _bills.Add(new Bill(_tenantId, _category.Id, "Aluguel julho", 1200m, new DateOnly(2026, 7, 10)));
        var handler = CreateHandler();

        var result = await handler.Handle(new ListBillsQuery(2026, 6), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("Aluguel junho", item.Description);
    }

    [Fact]
    public async Task Handle_ShouldNotReturnBillsFromOtherTenants()
    {
        _bills.Add(new Bill(_tenantId, _category.Id, "Minha conta", 100m, new DateOnly(2026, 6, 10)));
        _bills.Add(new Bill(Guid.NewGuid(), Guid.NewGuid(), "Conta alheia", 100m, new DateOnly(2026, 6, 10)));
        var handler = CreateHandler();

        var result = await handler.Handle(new ListBillsQuery(2026, 6), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("Minha conta", item.Description);
    }

    [Fact]
    public async Task Handle_ShouldComputeStatusAndJoinNames()
    {
        var account = new FinancialAccount(_tenantId, "Banco", FinancialAccountType.CheckingAccount, 500m);
        _accounts.Add(account);

        var overdue = new Bill(_tenantId, _category.Id, "Vencida", 100m, new DateOnly(2026, 6, 5));
        var pending = new Bill(_tenantId, _category.Id, "Pendente", 100m, new DateOnly(2026, 6, 20), account.Id);
        var paid = new Bill(_tenantId, _category.Id, "Paga", 100m, new DateOnly(2026, 6, 5));
        paid.MarkAsPaid(new DateOnly(2026, 6, 4), null, DateTime.UtcNow);
        _bills.Add(overdue);
        _bills.Add(pending);
        _bills.Add(paid);

        var handler = CreateHandler(new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc));

        var result = await handler.Handle(new ListBillsQuery(2026, 6), CancellationToken.None);

        Assert.Equal(BillStatus.Overdue, result.Single(bill => bill.Description == "Vencida").Status);
        Assert.Equal(BillStatus.Pending, result.Single(bill => bill.Description == "Pendente").Status);
        Assert.Equal(BillStatus.Paid, result.Single(bill => bill.Description == "Paga").Status);

        var pendingItem = result.Single(bill => bill.Description == "Pendente");
        Assert.Equal("Moradia", pendingItem.CategoryName);
        Assert.Equal("#FF0000", pendingItem.CategoryColor);
        Assert.Equal("Banco", pendingItem.PaymentAccountName);
    }
}
