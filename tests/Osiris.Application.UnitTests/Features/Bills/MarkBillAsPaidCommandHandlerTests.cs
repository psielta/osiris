using Osiris.Application.Common.Models;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;
using Osiris.Application.UnitTests.Features.Bills.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Bills;

public sealed class MarkBillAsPaidCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeFinancialAccountMovementRepository _movements = new();
    private readonly FakeBillRepository _bills;
    private readonly FakeFinancialAccountRepository _accounts = new();

    private readonly Bill _bill;

    public MarkBillAsPaidCommandHandlerTests()
    {
        _bills = new FakeBillRepository(_movements);
        _bill = new Bill(_tenantId, Guid.NewGuid(), "Aluguel", 1200m, new DateOnly(2026, 6, 10));
        _bills.Add(_bill);
    }

    private MarkBillAsPaidCommandHandler CreateHandler()
    {
        return new MarkBillAsPaidCommandHandler(
            _bills,
            _accounts,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider());
    }

    private MarkBillAsPaidCommand Command(Guid? accountId = null, Guid? billId = null)
    {
        return new MarkBillAsPaidCommand(billId ?? _bill.Id, new DateOnly(2026, 6, 8), accountId);
    }

    private FinancialAccount SeedAccount(decimal initialBalance = 5000m)
    {
        var account = new FinancialAccount(_tenantId, "Banco", FinancialAccountType.CheckingAccount, initialBalance);
        _accounts.Add(account);
        return account;
    }

    [Fact]
    public async Task Handle_WithoutAccount_ShouldMarkPaidWithoutMovement()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 6, 8), _bill.PaidAt);
        Assert.Empty(_movements.Movements);
    }

    [Fact]
    public async Task Handle_WithAccount_ShouldCreateBillPaymentMovement()
    {
        var account = SeedAccount();
        var handler = CreateHandler();

        var result = await handler.Handle(Command(accountId: account.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var movement = Assert.Single(_movements.Movements);
        Assert.Equal(FinancialAccountMovementType.BillPayment, movement.Type);
        Assert.Equal(1200m, movement.Amount);
        Assert.Equal(account.Id, movement.FinancialAccountId);
        Assert.Equal(new DateOnly(2026, 6, 8), movement.OccurredOn);

        // The categorized expense lives on the bill; the movement is cash flow only.
        Assert.Null(movement.CategoryId);
        Assert.Equal(nameof(Bill), movement.RelatedEntityType);
        Assert.Equal(_bill.Id, movement.RelatedEntityId);
    }

    [Fact]
    public async Task Handle_WithAccount_ShouldReduceAccountBalance()
    {
        var account = SeedAccount(initialBalance: 5000m);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(accountId: account.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3800m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WithAccount_ShouldStoreAccountOnBill()
    {
        var account = SeedAccount();
        var handler = CreateHandler();

        await handler.Handle(Command(accountId: account.Id), CancellationToken.None);

        Assert.Equal(account.Id, _bill.PaymentAccountId);
    }

    [Fact]
    public async Task Handle_WhenAlreadyPaid_ShouldReject()
    {
        _bill.MarkAsPaid(new DateOnly(2026, 6, 5), null, DateTime.UtcNow);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(new DateOnly(2026, 6, 5), _bill.PaidAt);
        Assert.Empty(_movements.Movements);
    }

    [Fact]
    public async Task Handle_WhenBillFromAnotherTenant_ShouldReturnNotFound()
    {
        var foreignBill = new Bill(Guid.NewGuid(), Guid.NewGuid(), "Internet", 100m, new DateOnly(2026, 6, 5));
        _bills.Add(foreignBill);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(billId: foreignBill.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
        Assert.Null(foreignBill.PaidAt);
    }

    [Fact]
    public async Task Handle_WhenAccountFromAnotherTenant_ShouldRejectWithoutPaying()
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
        Assert.Null(_bill.PaidAt);
        Assert.Equal(500m, foreignAccount.CurrentBalance);
        Assert.Empty(_movements.Movements);
    }

    [Fact]
    public async Task Handle_WhenAccountArchived_ShouldReject()
    {
        var account = SeedAccount();
        account.Archive(DateTime.UtcNow);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(accountId: account.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(_bill.PaidAt);
        Assert.Equal(5000m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenPaidAtMissing_ShouldReject()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new MarkBillAsPaidCommand(_bill.Id, PaidAt: null, PaymentAccountId: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(_bill.PaidAt);
    }
}
