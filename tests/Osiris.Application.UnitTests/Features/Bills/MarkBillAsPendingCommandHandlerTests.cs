using Osiris.Application.Common.Models;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPending;
using Osiris.Application.UnitTests.Features.Bills.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Bills;

public sealed class MarkBillAsPendingCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeFinancialAccountMovementRepository _movements = new();
    private readonly FakeBillRepository _bills;
    private readonly FakeFinancialAccountRepository _accounts = new();

    private readonly Bill _bill;

    public MarkBillAsPendingCommandHandlerTests()
    {
        _bills = new FakeBillRepository(_movements);
        _bill = new Bill(_tenantId, Guid.NewGuid(), "Aluguel", 1200m, new DateOnly(2026, 6, 10));
        _bills.Add(_bill);
    }

    private MarkBillAsPendingCommandHandler CreateHandler()
    {
        return new MarkBillAsPendingCommandHandler(
            _bills,
            _movements,
            _accounts,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider());
    }

    private async Task<FinancialAccount> PayBillFromAccountAsync(decimal initialBalance = 5000m)
    {
        var account = new FinancialAccount(_tenantId, "Banco", FinancialAccountType.CheckingAccount, initialBalance);
        _accounts.Add(account);

        var payHandler = new MarkBillAsPaidCommandHandler(
            _bills,
            _accounts,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider());
        var payment = await payHandler.Handle(
            new MarkBillAsPaidCommand(_bill.Id, new DateOnly(2026, 6, 8), account.Id),
            CancellationToken.None);
        Assert.True(payment.IsSuccess);

        return account;
    }

    [Fact]
    public async Task Handle_ShouldClearPaidAt()
    {
        _bill.MarkAsPaid(new DateOnly(2026, 6, 8), null, DateTime.UtcNow);
        var handler = CreateHandler();

        var result = await handler.Handle(new MarkBillAsPendingCommand(_bill.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(_bill.PaidAt);
    }

    [Fact]
    public async Task Handle_WhenPaidFromAccount_ShouldRemoveMovementAndRestoreBalance()
    {
        var account = await PayBillFromAccountAsync(initialBalance: 5000m);
        Assert.Equal(3800m, account.CurrentBalance);
        Assert.Single(_movements.Movements);
        var handler = CreateHandler();

        var result = await handler.Handle(new MarkBillAsPendingCommand(_bill.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(_bill.PaidAt);
        Assert.Empty(_movements.Movements);
        Assert.Equal(5000m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenPaidWithoutAccount_ShouldJustClearPaidAt()
    {
        _bill.MarkAsPaid(new DateOnly(2026, 6, 8), null, DateTime.UtcNow);
        var handler = CreateHandler();

        var result = await handler.Handle(new MarkBillAsPendingCommand(_bill.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(_bill.PaidAt);
        Assert.Empty(_movements.Movements);
    }

    [Fact]
    public async Task Handle_WhenAlreadyPending_ShouldReject()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new MarkBillAsPendingCommand(_bill.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_WhenBillFromAnotherTenant_ShouldReturnNotFound()
    {
        var foreignBill = new Bill(Guid.NewGuid(), Guid.NewGuid(), "Internet", 100m, new DateOnly(2026, 6, 5));
        foreignBill.MarkAsPaid(new DateOnly(2026, 6, 6), null, DateTime.UtcNow);
        _bills.Add(foreignBill);
        var handler = CreateHandler();

        var result = await handler.Handle(new MarkBillAsPendingCommand(foreignBill.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
        Assert.NotNull(foreignBill.PaidAt);
    }
}
