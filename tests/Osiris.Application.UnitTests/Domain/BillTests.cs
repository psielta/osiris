using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Domain;

public sealed class BillTests
{
    private static Bill CreateBill(Guid? tenantId = null)
    {
        return new Bill(
            tenantId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            "Aluguel",
            1200m,
            new DateOnly(2026, 6, 10));
    }

    [Fact]
    public void Constructor_ShouldStartPendingWithTrimmedDescription()
    {
        var bill = new Bill(Guid.NewGuid(), Guid.NewGuid(), "  Internet  ", 99.90m, new DateOnly(2026, 6, 15));

        Assert.Equal("Internet", bill.Description);
        Assert.Null(bill.PaidAt);
        Assert.False(bill.IsPaid);
    }

    [Fact]
    public void Constructor_WhenTenantEmpty_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new Bill(Guid.Empty, Guid.NewGuid(), "Aluguel", 100m, new DateOnly(2026, 6, 10)));
    }

    [Fact]
    public void Constructor_WhenCategoryEmpty_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new Bill(Guid.NewGuid(), Guid.Empty, "Aluguel", 100m, new DateOnly(2026, 6, 10)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_WhenAmountNotPositive_ShouldThrow(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Bill(Guid.NewGuid(), Guid.NewGuid(), "Aluguel", amount, new DateOnly(2026, 6, 10)));
    }

    [Fact]
    public void Constructor_WhenDescriptionBlank_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new Bill(Guid.NewGuid(), Guid.NewGuid(), "   ", 100m, new DateOnly(2026, 6, 10)));
    }

    [Fact]
    public void CalculateStatus_WhenPaid_ShouldBePaidEvenPastDueDate()
    {
        var status = Bill.CalculateStatus(
            paidAt: new DateOnly(2026, 6, 20),
            dueDate: new DateOnly(2026, 6, 10),
            today: new DateOnly(2026, 7, 1));

        Assert.Equal(BillStatus.Paid, status);
    }

    [Fact]
    public void CalculateStatus_WhenUnpaidBeforeDueDate_ShouldBePending()
    {
        var status = Bill.CalculateStatus(
            paidAt: null,
            dueDate: new DateOnly(2026, 6, 10),
            today: new DateOnly(2026, 6, 9));

        Assert.Equal(BillStatus.Pending, status);
    }

    [Fact]
    public void CalculateStatus_OnDueDate_ShouldStillBePending()
    {
        var status = Bill.CalculateStatus(
            paidAt: null,
            dueDate: new DateOnly(2026, 6, 10),
            today: new DateOnly(2026, 6, 10));

        Assert.Equal(BillStatus.Pending, status);
    }

    [Fact]
    public void CalculateStatus_WhenUnpaidPastDueDate_ShouldBeOverdue()
    {
        var status = Bill.CalculateStatus(
            paidAt: null,
            dueDate: new DateOnly(2026, 6, 10),
            today: new DateOnly(2026, 6, 11));

        Assert.Equal(BillStatus.Overdue, status);
    }

    [Fact]
    public void MarkAsPaid_ShouldStorePaidAtAndActualAccount()
    {
        var bill = CreateBill();
        var accountId = Guid.NewGuid();
        var now = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

        bill.MarkAsPaid(new DateOnly(2026, 6, 8), accountId, now);

        Assert.Equal(new DateOnly(2026, 6, 8), bill.PaidAt);
        Assert.Equal(accountId, bill.PaymentAccountId);
        Assert.Equal(now, bill.UpdatedAtUtc);
    }

    [Fact]
    public void MarkAsPaid_WithoutAccount_ShouldClearPlannedAccount()
    {
        var bill = new Bill(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Internet",
            99.90m,
            new DateOnly(2026, 6, 15),
            paymentAccountId: Guid.NewGuid());

        bill.MarkAsPaid(new DateOnly(2026, 6, 8), paymentAccountId: null, DateTime.UtcNow);

        Assert.Null(bill.PaymentAccountId);
    }

    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_ShouldThrow()
    {
        var bill = CreateBill();
        bill.MarkAsPaid(new DateOnly(2026, 6, 8), null, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => bill.MarkAsPaid(new DateOnly(2026, 6, 9), null, DateTime.UtcNow));
    }

    [Fact]
    public void MarkAsPending_ShouldClearPaidAt()
    {
        var bill = CreateBill();
        bill.MarkAsPaid(new DateOnly(2026, 6, 8), null, DateTime.UtcNow);

        bill.MarkAsPending(DateTime.UtcNow);

        Assert.Null(bill.PaidAt);
        Assert.False(bill.IsPaid);
    }

    [Fact]
    public void MarkAsPending_WhenNotPaid_ShouldThrow()
    {
        var bill = CreateBill();

        Assert.Throws<InvalidOperationException>(() => bill.MarkAsPending(DateTime.UtcNow));
    }
}
