using System.Text.Json;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.AiAssistant.Tools.Proposals;
using Osiris.Application.Features.Categories.DTOs;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Application.Features.CreditCardPurchases.DTOs;
using Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class ProposeCategoryChangeToolTests
{
    private static AiAgentContext Context(Guid tenantId) =>
        new(tenantId, "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 23), WritesEnabled: true);

    private static JsonElement Args(Guid purchaseId, Guid categoryId) =>
        JsonDocument.Parse($"{{\"creditCardPurchaseId\":\"{purchaseId}\",\"categoryId\":\"{categoryId}\"}}").RootElement;

    private static CreditCardPurchaseDetailsDto Purchase(Guid id, Guid currentCategoryId) =>
        new(id, Guid.NewGuid(), "Inter", "Mercado", currentCategoryId, "Farmácia", 80m,
            new DateOnly(2026, 6, 18), 1, null, Array.Empty<CreditCardPurchaseInstallmentDto>());

    private static IReadOnlyCollection<CategoryListItemDto> Categories(Guid expenseId) =>
        new[] { new CategoryListItemDto(expenseId, "Saúde", CategoryType.Expense, null, true) };

    [Fact]
    public async Task Creates_a_pending_category_change_proposal_without_executing()
    {
        var tenantId = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();
        var currentCategoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();

        var repo = new FakeAiActionProposalRepository();
        var factory = new AiActionProposalFactory(repo, new FakeDateTimeProvider(), Options.Create(new AiAgentOptions()));
        var sender = new MapSender()
            .On<GetCreditCardPurchaseDetailsQuery>(_ => Purchase(purchaseId, currentCategoryId))
            .On<ListCategoriesQuery>(_ => Categories(newCategoryId));
        var tool = new ProposeCategoryChangeTool(sender, factory);

        var result = await tool.ExecuteAsync(Args(purchaseId, newCategoryId), Context(tenantId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AiToolRisk.WriteProposal, tool.Risk);
        Assert.Single(result.Proposals!);

        var proposal = Assert.Single(repo.Proposals);
        Assert.Equal(AiActionTypes.CategoryChange, proposal.ActionType);
        Assert.Equal(AiActionProposalStatus.Pending, proposal.Status);
        Assert.Equal(ProposalState.PurchaseCategoryHash(currentCategoryId), proposal.StateHash);
    }

    [Fact]
    public async Task Fails_when_purchase_already_has_the_target_category()
    {
        var sameCategoryId = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();

        var repo = new FakeAiActionProposalRepository();
        var factory = new AiActionProposalFactory(repo, new FakeDateTimeProvider(), Options.Create(new AiAgentOptions()));
        var sender = new MapSender()
            .On<GetCreditCardPurchaseDetailsQuery>(_ => Purchase(purchaseId, sameCategoryId));
        var tool = new ProposeCategoryChangeTool(sender, factory);

        var result = await tool.ExecuteAsync(Args(purchaseId, sameCategoryId), Context(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(repo.Proposals);
    }
}
