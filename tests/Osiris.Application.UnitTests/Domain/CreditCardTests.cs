using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Domain;

public sealed class CreditCardTests
{
    [Fact]
    public void Constructor_ShouldSetFieldsAndActivate()
    {
        var tenantId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var card = new CreditCard(tenantId, " Nubank ", 1500m, 3, 10, accountId);

        Assert.Equal(tenantId, card.TenantId);
        Assert.Equal("Nubank", card.Name);
        Assert.Equal("NUBANK", card.NormalizedName);
        Assert.Equal(1500m, card.Limit);
        Assert.Equal(3, card.ClosingDay);
        Assert.Equal(10, card.DueDay);
        Assert.Equal(accountId, card.PaymentAccountId);
        Assert.True(card.IsActive);
    }

    [Fact]
    public void Constructor_WhenTenantEmpty_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new CreditCard(Guid.Empty, "Nubank", 0m, 1, 1, null));
    }

    [Fact]
    public void Constructor_WhenNameMissing_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new CreditCard(Guid.NewGuid(), "   ", 0m, 1, 1, null));
    }

    [Fact]
    public void Constructor_WhenLimitNegative_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CreditCard(Guid.NewGuid(), "Nubank", -1m, 1, 1, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Constructor_WhenClosingDayOutOfRange_ShouldThrow(int day)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CreditCard(Guid.NewGuid(), "Nubank", 0m, day, 10, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Constructor_WhenDueDayOutOfRange_ShouldThrow(int day)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CreditCard(Guid.NewGuid(), "Nubank", 0m, 10, day, null));
    }

    [Fact]
    public void Update_ShouldChangeFieldsAndStampUpdatedAt()
    {
        var card = new CreditCard(Guid.NewGuid(), "Nubank", 1000m, 3, 10, null);
        var now = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);
        var accountId = Guid.NewGuid();

        card.Update(" Inter ", 2000m, 5, 15, accountId, now);

        Assert.Equal("Inter", card.Name);
        Assert.Equal("INTER", card.NormalizedName);
        Assert.Equal(2000m, card.Limit);
        Assert.Equal(5, card.ClosingDay);
        Assert.Equal(15, card.DueDay);
        Assert.Equal(accountId, card.PaymentAccountId);
        Assert.Equal(now, card.UpdatedAtUtc);
    }

    [Fact]
    public void Archive_ShouldDeactivateAndStampUpdatedAt()
    {
        var card = new CreditCard(Guid.NewGuid(), "Nubank", 0m, 1, 1, null);
        var now = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

        card.Archive(now);

        Assert.False(card.IsActive);
        Assert.Equal(now, card.UpdatedAtUtc);
    }
}
