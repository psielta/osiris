using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Domain;

public sealed class CreditCardStatementTests
{
    private static readonly DateOnly Closing = new(2026, 6, 25);
    private static readonly DateOnly Due = new(2026, 7, 5);

    [Fact]
    public void CalculateStatus_WhenNoPaymentBeforeClosing_ShouldBeOpen()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 0m, Closing, Due, new DateOnly(2026, 6, 20));

        Assert.Equal(CreditCardStatementStatus.Open, status);
    }

    [Fact]
    public void CalculateStatus_WhenNoPaymentOnClosingDay_ShouldBeOpen()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 0m, Closing, Due, Closing);

        Assert.Equal(CreditCardStatementStatus.Open, status);
    }

    [Fact]
    public void CalculateStatus_WhenNoPaymentAfterClosingBeforeDue_ShouldBeClosed()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 0m, Closing, Due, new DateOnly(2026, 6, 26));

        Assert.Equal(CreditCardStatementStatus.Closed, status);
    }

    [Fact]
    public void CalculateStatus_WhenPartiallyPaid_ShouldBePartiallyPaid()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 40m, Closing, Due, new DateOnly(2026, 6, 26));

        Assert.Equal(CreditCardStatementStatus.PartiallyPaid, status);
    }

    [Fact]
    public void CalculateStatus_WhenFullyPaid_ShouldBePaid()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 100m, Closing, Due, new DateOnly(2026, 6, 26));

        Assert.Equal(CreditCardStatementStatus.Paid, status);
    }

    [Fact]
    public void CalculateStatus_WhenOverpaid_ShouldBePaid()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 150m, Closing, Due, new DateOnly(2026, 6, 26));

        Assert.Equal(CreditCardStatementStatus.Paid, status);
    }

    [Fact]
    public void CalculateStatus_WhenPastDueWithOpenBalance_ShouldBeOverdue()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 0m, Closing, Due, new DateOnly(2026, 7, 6));

        Assert.Equal(CreditCardStatementStatus.Overdue, status);
    }

    [Fact]
    public void CalculateStatus_WhenPastDuePartiallyPaid_ShouldBeOverdue()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 40m, Closing, Due, new DateOnly(2026, 7, 6));

        Assert.Equal(CreditCardStatementStatus.Overdue, status);
    }

    [Fact]
    public void CalculateStatus_WhenPastDueFullyPaid_ShouldBePaid()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 100m, Closing, Due, new DateOnly(2026, 7, 6));

        Assert.Equal(CreditCardStatementStatus.Paid, status);
    }

    [Fact]
    public void CalculateStatus_WhenOnDueDateWithOpenBalance_ShouldNotBeOverdue()
    {
        var status = CreditCardStatement.CalculateStatus(100m, 0m, Closing, Due, Due);

        Assert.Equal(CreditCardStatementStatus.Closed, status);
    }

    [Fact]
    public void CalculateStatus_WhenEmptyStatementPastClosing_ShouldBeClosed()
    {
        var status = CreditCardStatement.CalculateStatus(0m, 0m, Closing, Due, new DateOnly(2026, 7, 10));

        Assert.Equal(CreditCardStatementStatus.Closed, status);
    }

    [Fact]
    public void RefreshStatus_WhenStatusChanges_ShouldPersistAndStampUpdatedAt()
    {
        var statement = CreateStatement();
        var utcNow = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

        statement.RefreshStatus(100m, 0m, new DateOnly(2026, 7, 6), utcNow);

        Assert.Equal(CreditCardStatementStatus.Overdue, statement.Status);
        Assert.Equal(utcNow, statement.UpdatedAtUtc);
    }

    [Fact]
    public void RefreshStatus_WhenStatusUnchanged_ShouldNotStampUpdatedAt()
    {
        var statement = CreateStatement();

        statement.RefreshStatus(100m, 0m, new DateOnly(2026, 6, 20), new DateTime(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(CreditCardStatementStatus.Open, statement.Status);
        Assert.Null(statement.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WhenReferenceMonthInvalid_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CreditCardStatement(Guid.NewGuid(), Guid.NewGuid(), 13, 2026, Closing, Due));
    }

    [Fact]
    public void Constructor_WhenDueDateBeforeClosingDate_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new CreditCardStatement(Guid.NewGuid(), Guid.NewGuid(), 6, 2026, Closing, Closing.AddDays(-1)));
    }

    private static CreditCardStatement CreateStatement()
    {
        return new CreditCardStatement(Guid.NewGuid(), Guid.NewGuid(), 6, 2026, Closing, Due);
    }
}
