using System.Text.Json;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.AiAssistant.Tools.Proposals;
using Osiris.Application.Features.Bills.DTOs;
using Osiris.Application.Features.Bills.Queries.GetBillDetails;
using Osiris.Application.Features.Bills.Queries.GetBillForEdit;
using Osiris.Application.Features.CreditCards.DTOs;
using Osiris.Application.Features.CreditCards.Queries.GetCreditCardForEdit;
using Osiris.Application.Features.CreditCards.Queries.ListCreditCards;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class CardAndBillProposalToolsTests
{
    private static AiAgentContext Context() =>
        new(Guid.NewGuid(), "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 23), WritesEnabled: true);

    private static JsonElement Args(string json) => JsonDocument.Parse(json).RootElement;

    private static (FakeAiActionProposalRepository Repo, AiActionProposalFactory Factory) NewFactory()
    {
        var repo = new FakeAiActionProposalRepository();
        return (repo, new AiActionProposalFactory(repo, new FakeDateTimeProvider(), Options.Create(new AiAgentOptions())));
    }

    [Fact]
    public async Task Card_creation_creates_a_pending_proposal()
    {
        var (repo, factory) = NewFactory();
        var tool = new ProposeCardCreationTool(factory);

        var result = await tool.ExecuteAsync(
            Args("{\"name\":\"Inter\",\"limit\":3000,\"closingDay\":1,\"dueDay\":7}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AiActionTypes.CardCreation, Assert.Single(repo.Proposals).ActionType);
    }

    [Fact]
    public async Task Card_update_preserves_payment_account_and_hashes_current()
    {
        var cardId = Guid.NewGuid();
        var paymentAccountId = Guid.NewGuid();
        var current = new CreditCardEditDto(cardId, "Inter", 3000m, 1, 7, paymentAccountId);
        var (repo, factory) = NewFactory();
        var sender = new MapSender().On<GetCreditCardForEditQuery>(_ => current);
        var tool = new ProposeCardUpdateTool(sender, factory);

        var result = await tool.ExecuteAsync(Args($"{{\"creditCardId\":\"{cardId}\",\"limit\":5000}}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var proposal = Assert.Single(repo.Proposals);
        Assert.Equal(AiActionTypes.CardUpdate, proposal.ActionType);
        Assert.Equal(ProposalState.CardHash("Inter", 3000m, 1, 7), proposal.StateHash);
        Assert.Contains(paymentAccountId.ToString(), proposal.PayloadJson);
    }

    [Fact]
    public async Task Card_archive_refuses_archived()
    {
        var cardId = Guid.NewGuid();
        var archived = new CreditCardListItemDto(cardId, "Velho", 1000m, 1, 10, false);
        var (repo, factory) = NewFactory();
        var sender = new MapSender().On<ListCreditCardsQuery>(_ => (IReadOnlyCollection<CreditCardListItemDto>)new[] { archived });
        var tool = new ProposeCardArchiveTool(sender, factory);

        var result = await tool.ExecuteAsync(Args($"{{\"creditCardId\":\"{cardId}\"}}"), Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(repo.Proposals);
    }

    [Fact]
    public async Task Bill_update_keeps_unspecified_fields()
    {
        var billId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var current = new BillEditDto(billId, "Aluguel", 1200m, new DateOnly(2026, 7, 10), categoryId, null, null, IsPaid: false);
        var (repo, factory) = NewFactory();
        var sender = new MapSender().On<GetBillForEditQuery>(_ => current);
        var tool = new ProposeBillUpdateTool(sender, factory);

        var result = await tool.ExecuteAsync(Args($"{{\"billId\":\"{billId}\",\"amount\":1300}}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var proposal = Assert.Single(repo.Proposals);
        Assert.Equal(AiActionTypes.BillUpdate, proposal.ActionType);
        Assert.Equal(
            ProposalState.BillEditHash("Aluguel", 1200m, new DateOnly(2026, 7, 10), categoryId, false),
            proposal.StateHash);
    }

    [Fact]
    public async Task Bill_unpay_refuses_a_bill_that_is_not_paid()
    {
        var billId = Guid.NewGuid();
        var pending = new BillDetailsDto(
            billId, "Luz", 200m, new DateOnly(2026, 6, 10), null, BillStatus.Pending,
            Guid.NewGuid(), null, null, null, null, null, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), null);
        var (repo, factory) = NewFactory();
        var sender = new MapSender().On<GetBillDetailsQuery>(_ => pending);
        var tool = new ProposeBillUnpayTool(sender, factory);

        var result = await tool.ExecuteAsync(Args($"{{\"billId\":\"{billId}\"}}"), Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(repo.Proposals);
    }
}
