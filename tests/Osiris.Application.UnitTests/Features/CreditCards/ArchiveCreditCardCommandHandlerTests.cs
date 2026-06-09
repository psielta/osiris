using Osiris.Application.Features.CreditCards.Commands.ArchiveCreditCard;
using Osiris.Application.UnitTests.Features.CreditCards.Support;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.CreditCards;

public sealed class ArchiveCreditCardCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenExists_ShouldArchive()
    {
        var tenantId = Guid.NewGuid();
        var card = new CreditCard(tenantId, "Nubank", 0m, 1, 1, null);
        var cards = new FakeCreditCardRepository();
        cards.Add(card);
        var handler = new ArchiveCreditCardCommandHandler(cards, new FakeCurrentUser(tenantId), new FakeDateTimeProvider());

        var result = await handler.Handle(new ArchiveCreditCardCommand(card.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(card.IsActive);
    }

    [Fact]
    public async Task Handle_WhenCardBelongsToOtherTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var card = new CreditCard(Guid.NewGuid(), "Nubank", 0m, 1, 1, null);
        var cards = new FakeCreditCardRepository();
        cards.Add(card);
        var handler = new ArchiveCreditCardCommandHandler(cards, new FakeCurrentUser(tenantId), new FakeDateTimeProvider());

        var result = await handler.Handle(new ArchiveCreditCardCommand(card.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.True(card.IsActive);
    }
}
