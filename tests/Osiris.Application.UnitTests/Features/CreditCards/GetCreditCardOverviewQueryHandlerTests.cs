using Osiris.Application.Features.CreditCards.Queries.GetCreditCardOverview;
using Osiris.Application.UnitTests.Features.CreditCards.Support;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.CreditCards;

public sealed class GetCreditCardOverviewQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeCreditCardRepository _cards = new();
    private readonly FakeCreditCardStatementPaymentRepository _payments = new();
    private readonly FakeCreditCardStatementRepository _statements;

    private readonly CreditCard _card;

    public GetCreditCardOverviewQueryHandlerTests()
    {
        _statements = new FakeCreditCardStatementRepository(_payments);

        // Closing day 25 / due day 5: on 2026-06-08 the current cycle is June (due 2026-07-05).
        _card = new CreditCard(_tenantId, "Nubank", 1000m, 25, 5, null);
        _cards.Add(_card);
    }

    private GetCreditCardOverviewQueryHandler CreateHandler()
    {
        return new GetCreditCardOverviewQueryHandler(
            _cards,
            _statements,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc) });
    }

    private CreditCardStatement SeedStatement(int month, int year, decimal total)
    {
        var closing = new DateOnly(year, month, 25);
        var due = closing.AddMonths(1).AddDays(5 - 25);
        var statement = new CreditCardStatement(
            _tenantId,
            _card.Id,
            month,
            year,
            closing,
            new DateOnly(due.Year, due.Month, 5));
        _statements.Add(statement, total);
        return statement;
    }

    [Fact]
    public async Task Handle_ShouldSumOpenBalancesAsUsedLimit()
    {
        SeedStatement(5, 2026, 100m);
        var june = SeedStatement(6, 2026, 200m);
        SeedStatement(7, 2026, 300m);
        _payments.Add(new CreditCardStatementPayment(_tenantId, june.Id, null, 50m, new DateOnly(2026, 6, 7)));

        var result = await CreateHandler().Handle(new GetCreditCardOverviewQuery(_card.Id), CancellationToken.None);

        Assert.NotNull(result);

        // 100 + (200 - 50) + 300 = 550 of the 1000 limit.
        Assert.Equal(550m, result.UsedLimit);
        Assert.Equal(450m, result.AvailableLimit);
        Assert.Equal(55m, result.UsagePercentage);
    }

    [Fact]
    public async Task Handle_ShouldExposeNextStatementAndFutureTotal()
    {
        SeedStatement(6, 2026, 200m);
        var july = SeedStatement(7, 2026, 300m);
        SeedStatement(8, 2026, 400m);

        var result = await CreateHandler().Handle(new GetCreditCardOverviewQuery(_card.Id), CancellationToken.None);

        Assert.NotNull(result);

        // Current cycle is June; July is "next" and July + August are future commitments.
        Assert.Equal(july.Id, result.NextStatement?.Id);
        Assert.Equal(300m, result.NextStatement?.TotalAmount);
        Assert.Equal(700m, result.FutureInstallmentsTotal);
    }

    [Fact]
    public async Task Handle_WithoutStatements_ShouldReturnZeroedOverview()
    {
        var result = await CreateHandler().Handle(new GetCreditCardOverviewQuery(_card.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0m, result.UsedLimit);
        Assert.Equal(1000m, result.AvailableLimit);
        Assert.Null(result.NextStatement);
        Assert.Equal(0m, result.FutureInstallmentsTotal);
    }

    [Fact]
    public async Task Handle_WhenCardFromAnotherTenant_ShouldReturnNull()
    {
        var foreignCard = new CreditCard(Guid.NewGuid(), "Alheio", 1000m, 25, 5, null);
        _cards.Add(foreignCard);

        var result = await CreateHandler().Handle(new GetCreditCardOverviewQuery(foreignCard.Id), CancellationToken.None);

        Assert.Null(result);
    }
}
