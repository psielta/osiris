using Osiris.Application.Features.CreditCardPurchases.Queries.GetPurchasePreview;
using Osiris.Application.UnitTests.Features.CreditCardPurchases.Support;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases;

public sealed class GetPurchasePreviewQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeCreditCardRepository _cards = new();
    private readonly FakeCreditCardInstallmentRepository _installments = new();
    private readonly FakeCreditCardStatementRepository _statements;

    private readonly CreditCard _card;

    public GetPurchasePreviewQueryHandlerTests()
    {
        _statements = new FakeCreditCardStatementRepository(_installments);

        // Closing day 25 / due day 5: purchases up to the 25th enter the month's statement.
        _card = new CreditCard(_tenantId, "Nubank", 1000m, 25, 5, null);
        _cards.Add(_card);
    }

    private GetPurchasePreviewQueryHandler CreateHandler()
    {
        return new GetPurchasePreviewQueryHandler(
            _cards,
            _statements,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc) });
    }

    private GetPurchasePreviewQuery Query(
        decimal? totalAmount = 300m,
        int? installments = 3,
        DateOnly? purchaseDate = null,
        Guid? cardId = null)
    {
        return new GetPurchasePreviewQuery(
            cardId ?? _card.Id,
            totalAmount,
            purchaseDate ?? new DateOnly(2026, 6, 20),
            installments);
    }

    [Fact]
    public async Task Handle_ShouldSplitAmountWithRemainderOnLastInstallment()
    {
        var result = await CreateHandler().Handle(Query(totalAmount: 100m, installments: 3), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, result.Installments.Select(i => i.Amount));
        Assert.Equal(100m, result.TotalAmount);
    }

    [Fact]
    public async Task Handle_ShouldProjectImpactedStatementsMonthByMonth()
    {
        var result = await CreateHandler().Handle(
            Query(totalAmount: 300m, installments: 3, purchaseDate: new DateOnly(2026, 6, 20)),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(
            new[] { (6, 2026), (7, 2026), (8, 2026) },
            result.Installments.Select(i => (i.ReferenceMonth, i.ReferenceYear)));
    }

    [Fact]
    public async Task Handle_PurchaseAfterClosingDay_ShouldStartOnNextStatement()
    {
        var result = await CreateHandler().Handle(
            Query(totalAmount: 100m, installments: 1, purchaseDate: new DateOnly(2026, 6, 26)),
            CancellationToken.None);

        Assert.NotNull(result);
        var installment = Assert.Single(result.Installments);
        Assert.Equal(7, installment.ReferenceMonth);
        Assert.Equal(2026, installment.ReferenceYear);
    }

    [Fact]
    public async Task Handle_WhenPurchasePushesUsageAbove80Percent_ShouldFlagHighUsage()
    {
        var result = await CreateHandler().Handle(Query(totalAmount: 850m, installments: 1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.HighLimitUsage);
        Assert.Equal(85m, result.ProjectedUsagePercentage);
    }

    [Fact]
    public async Task Handle_WhenUsageStaysBelow80Percent_ShouldNotFlag()
    {
        var result = await CreateHandler().Handle(Query(totalAmount: 500m, installments: 1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.HighLimitUsage);
    }

    [Fact]
    public async Task Handle_ShouldProjectFutureInstallmentsTotal()
    {
        // 3x of 100 on 2026-06-20: June lands in the current cycle, July and August are future.
        var result = await CreateHandler().Handle(Query(totalAmount: 300m, installments: 3), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(200m, result.ProjectedFutureInstallmentsTotal);
    }

    [Fact]
    public async Task Handle_WhenCardFromAnotherTenant_ShouldReturnNull()
    {
        var foreignCard = new CreditCard(Guid.NewGuid(), "Alheio", 1000m, 25, 5, null);
        _cards.Add(foreignCard);

        var result = await CreateHandler().Handle(Query(cardId: foreignCard.Id), CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    public async Task Handle_WhenAmountMissingOrInvalid_ShouldReturnNull(double? amount)
    {
        var result = await CreateHandler().Handle(
            Query(totalAmount: amount is null ? null : (decimal)amount.Value),
            CancellationToken.None);

        Assert.Null(result);
    }
}
