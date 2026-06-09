using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Domain;

public sealed class FinancialAccountTests
{
    [Fact]
    public void Constructor_ShouldStartCurrentBalanceEqualToInitialBalance()
    {
        var account = new FinancialAccount(Guid.NewGuid(), "Carteira", FinancialAccountType.Cash, 150.75m);

        Assert.Equal(150.75m, account.InitialBalance);
        Assert.Equal(150.75m, account.CurrentBalance);
        Assert.True(account.IsActive);
        Assert.Equal("CARTEIRA", account.NormalizedName);
    }

    [Fact]
    public void Constructor_WhenTenantEmpty_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new FinancialAccount(Guid.Empty, "Banco", FinancialAccountType.Cash, 0m));
    }

    [Fact]
    public void ApplyMovement_WhenInflow_ShouldIncreaseBalanceAndStampUpdatedAt()
    {
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 100m);
        var now = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

        account.ApplyMovement(FinancialAccountMovementType.Income, 50m, now);

        Assert.Equal(150m, account.CurrentBalance);
        Assert.Equal(now, account.UpdatedAtUtc);
    }

    [Fact]
    public void ApplyMovement_WhenOutflow_ShouldDecreaseBalance()
    {
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 100m);

        account.ApplyMovement(FinancialAccountMovementType.Expense, 30m, DateTime.UtcNow);

        Assert.Equal(70m, account.CurrentBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ApplyMovement_WhenAmountNotPositive_ShouldThrowAndKeepBalance(decimal amount)
    {
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 100m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => account.ApplyMovement(FinancialAccountMovementType.Income, amount, DateTime.UtcNow));
        Assert.Equal(100m, account.CurrentBalance);
    }

    [Fact]
    public void Update_ShouldChangeNameAndTypeWithoutTouchingBalance()
    {
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 100m);
        account.ApplyMovement(FinancialAccountMovementType.Income, 50m, DateTime.UtcNow);
        var now = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

        account.Update(" Reserva ", FinancialAccountType.SavingsAccount, now);

        Assert.Equal("Reserva", account.Name);
        Assert.Equal("RESERVA", account.NormalizedName);
        Assert.Equal(FinancialAccountType.SavingsAccount, account.Type);
        Assert.Equal(100m, account.InitialBalance);
        Assert.Equal(150m, account.CurrentBalance);
        Assert.Equal(now, account.UpdatedAtUtc);
    }

    [Fact]
    public void Archive_ShouldDeactivateAndStampUpdatedAt()
    {
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 0m);
        var now = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

        account.Archive(now);

        Assert.False(account.IsActive);
        Assert.Equal(now, account.UpdatedAtUtc);
    }

    [Fact]
    public void RevertMovement_WhenOutflow_ShouldAddAmountBack()
    {
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 100m);
        account.ApplyMovement(FinancialAccountMovementType.BillPayment, 30m, DateTime.UtcNow);

        account.RevertMovement(FinancialAccountMovementType.BillPayment, 30m, DateTime.UtcNow);

        Assert.Equal(100m, account.CurrentBalance);
    }

    [Fact]
    public void RevertMovement_WhenInflow_ShouldSubtractAmount()
    {
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 100m);
        account.ApplyMovement(FinancialAccountMovementType.Income, 30m, DateTime.UtcNow);

        account.RevertMovement(FinancialAccountMovementType.Income, 30m, DateTime.UtcNow);

        Assert.Equal(100m, account.CurrentBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RevertMovement_WhenAmountNotPositive_ShouldThrowAndKeepBalance(decimal amount)
    {
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 100m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => account.RevertMovement(FinancialAccountMovementType.BillPayment, amount, DateTime.UtcNow));
        Assert.Equal(100m, account.CurrentBalance);
    }
}
