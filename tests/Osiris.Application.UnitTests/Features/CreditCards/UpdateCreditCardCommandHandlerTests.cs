using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCards.Commands.UpdateCreditCard;
using Osiris.Application.UnitTests.Features.CreditCards.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.CreditCards;

public sealed class UpdateCreditCardCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateCard()
    {
        var tenantId = Guid.NewGuid();
        var card = new CreditCard(tenantId, "Nubank", 1000m, 3, 10, null);
        var cards = new FakeCreditCardRepository();
        cards.Add(card);
        var handler = new UpdateCreditCardCommandHandler(
            cards,
            new FakeFinancialAccountRepository(),
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(
            new UpdateCreditCardCommand(card.Id, "Inter", 2000m, 5, 15, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Inter", card.Name);
        Assert.Equal(2000m, card.Limit);
        Assert.Equal(5, card.ClosingDay);
        Assert.Equal(15, card.DueDay);
    }

    [Fact]
    public async Task Handle_WhenCardBelongsToOtherTenant_ShouldReturnNotFound()
    {
        var tenantId = Guid.NewGuid();
        var card = new CreditCard(Guid.NewGuid(), "Nubank", 1000m, 3, 10, null);
        var cards = new FakeCreditCardRepository();
        cards.Add(card);
        var handler = new UpdateCreditCardCommandHandler(
            cards,
            new FakeFinancialAccountRepository(),
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(
            new UpdateCreditCardCommand(card.Id, "Inter", 2000m, 5, 15, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
        Assert.Equal("Nubank", card.Name);
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameInSameTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var card = new CreditCard(tenantId, "Nubank", 0m, 1, 1, null);
        var other = new CreditCard(tenantId, "Inter", 0m, 1, 1, null);
        var cards = new FakeCreditCardRepository();
        cards.Add(card);
        cards.Add(other);
        var handler = new UpdateCreditCardCommandHandler(
            cards,
            new FakeFinancialAccountRepository(),
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(
            new UpdateCreditCardCommand(card.Id, "Inter", 0m, 1, 1, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Nubank", card.Name);
    }

    [Fact]
    public async Task Handle_WhenPaymentAccountFromAnotherTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var card = new CreditCard(tenantId, "Nubank", 0m, 1, 1, null);
        var cards = new FakeCreditCardRepository();
        cards.Add(card);
        var foreignAccount = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 0m);
        var accounts = new FakeFinancialAccountRepository();
        accounts.Add(foreignAccount);
        var handler = new UpdateCreditCardCommandHandler(
            cards,
            accounts,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(
            new UpdateCreditCardCommand(card.Id, "Nubank", 0m, 1, 1, foreignAccount.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(card.PaymentAccountId);
    }
}
