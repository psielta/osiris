using Osiris.Application.Features.CreditCards.Commands.CreateCreditCard;
using Osiris.Application.UnitTests.Features.CreditCards.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.CreditCards;

public sealed class CreateCreditCardCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_ShouldCreateCardForCurrentTenant()
    {
        var tenantId = Guid.NewGuid();
        var cards = new FakeCreditCardRepository();
        var handler = new CreateCreditCardCommandHandler(cards, new FakeFinancialAccountRepository(), new FakeCurrentUser(tenantId));

        var result = await handler.Handle(new CreateCreditCardCommand("Nubank", 1500m, 3, 10, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var card = Assert.Single(cards.CreditCards);
        Assert.Equal(result.Value, card.Id);
        Assert.Equal(tenantId, card.TenantId);
        Assert.Equal("Nubank", card.Name);
        Assert.Equal("NUBANK", card.NormalizedName);
        Assert.Equal(1500m, card.Limit);
        Assert.Equal(3, card.ClosingDay);
        Assert.Equal(10, card.DueDay);
        Assert.Null(card.PaymentAccountId);
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameInSameTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var cards = new FakeCreditCardRepository();
        cards.Add(new CreditCard(tenantId, "Nubank", 0m, 1, 1, null));
        var handler = new CreateCreditCardCommandHandler(cards, new FakeFinancialAccountRepository(), new FakeCurrentUser(tenantId));

        var result = await handler.Handle(new CreateCreditCardCommand("nubank", 0m, 1, 1, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(cards.CreditCards);
    }

    [Fact]
    public async Task Handle_WhenSameNameInDifferentTenant_ShouldCreate()
    {
        var tenantId = Guid.NewGuid();
        var cards = new FakeCreditCardRepository();
        cards.Add(new CreditCard(Guid.NewGuid(), "Nubank", 0m, 1, 1, null));
        var handler = new CreateCreditCardCommandHandler(cards, new FakeFinancialAccountRepository(), new FakeCurrentUser(tenantId));

        var result = await handler.Handle(new CreateCreditCardCommand("Nubank", 0m, 1, 1, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, cards.CreditCards.Count);
    }

    [Fact]
    public async Task Handle_WhenPaymentAccountSameTenant_ShouldCreateWithAccount()
    {
        var tenantId = Guid.NewGuid();
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        var accounts = new FakeFinancialAccountRepository();
        accounts.Add(account);
        var cards = new FakeCreditCardRepository();
        var handler = new CreateCreditCardCommandHandler(cards, accounts, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(new CreateCreditCardCommand("Nubank", 0m, 1, 1, account.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(account.Id, Assert.Single(cards.CreditCards).PaymentAccountId);
    }

    [Fact]
    public async Task Handle_WhenPaymentAccountFromAnotherTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var foreignAccount = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 0m);
        var accounts = new FakeFinancialAccountRepository();
        accounts.Add(foreignAccount);
        var cards = new FakeCreditCardRepository();
        var handler = new CreateCreditCardCommandHandler(cards, accounts, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(new CreateCreditCardCommand("Nubank", 0m, 1, 1, foreignAccount.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(cards.CreditCards);
    }

    [Fact]
    public async Task Handle_WhenPaymentAccountDoesNotExist_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var cards = new FakeCreditCardRepository();
        var handler = new CreateCreditCardCommandHandler(cards, new FakeFinancialAccountRepository(), new FakeCurrentUser(tenantId));

        var result = await handler.Handle(new CreateCreditCardCommand("Nubank", 0m, 1, 1, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(cards.CreditCards);
    }
}
