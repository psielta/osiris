using Osiris.Application.Common.Models;
using Osiris.Application.Features.Bills.Commands.DeleteBill;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;
using Osiris.Application.UnitTests.Features.Bills.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Bills;

public sealed class DeleteBillCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeFinancialAccountMovementRepository _movements = new();
    private readonly FakeBillRepository _bills;
    private readonly FakeFinancialAccountRepository _accounts = new();

    private readonly Bill _bill;

    public DeleteBillCommandHandlerTests()
    {
        _bills = new FakeBillRepository(_movements);
        _bill = new Bill(_tenantId, Guid.NewGuid(), "Aluguel", 1200m, new DateOnly(2026, 6, 10));
        _bills.Add(_bill);
    }

    private DeleteBillCommandHandler CreateHandler()
    {
        return new DeleteBillCommandHandler(
            _bills,
            _movements,
            _accounts,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider());
    }

    [Fact]
    public async Task Handle_ShouldDeletePendingBill()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteBillCommand(_bill.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_bills.Bills);
    }

    [Fact]
    public async Task Handle_WhenPaidFromAccount_ShouldRemoveMovementAndRestoreBalance()
    {
        var account = new FinancialAccount(_tenantId, "Banco", FinancialAccountType.CheckingAccount, 5000m);
        _accounts.Add(account);
        var payHandler = new MarkBillAsPaidCommandHandler(
            _bills,
            _accounts,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider());
        Assert.True((await payHandler.Handle(
            new MarkBillAsPaidCommand(_bill.Id, new DateOnly(2026, 6, 8), account.Id),
            CancellationToken.None)).IsSuccess);
        Assert.Equal(3800m, account.CurrentBalance);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteBillCommand(_bill.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_bills.Bills);
        Assert.Empty(_movements.Movements);
        Assert.Equal(5000m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenBillFromAnotherTenant_ShouldReturnNotFound()
    {
        var foreignBill = new Bill(Guid.NewGuid(), Guid.NewGuid(), "Internet", 100m, new DateOnly(2026, 6, 5));
        _bills.Add(foreignBill);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteBillCommand(foreignBill.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
        Assert.Contains(foreignBill, _bills.Bills);
    }
}
