using System.Text.Json;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.AiAssistant.Tools.Proposals;
using Osiris.Application.Features.Categories.DTOs;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class CategoryProposalToolsTests
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
    public async Task Creation_creates_a_pending_proposal()
    {
        var (repo, factory) = NewFactory();
        var tool = new ProposeCategoryCreationTool(factory);

        var result = await tool.ExecuteAsync(Args("{\"name\":\"Pets\",\"type\":\"expense\"}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var proposal = Assert.Single(repo.Proposals);
        Assert.Equal(AiActionTypes.CategoryCreation, proposal.ActionType);
    }

    [Fact]
    public async Task Creation_rejects_invalid_type()
    {
        var (_, factory) = NewFactory();
        var tool = new ProposeCategoryCreationTool(factory);

        var result = await tool.ExecuteAsync(Args("{\"name\":\"Pets\",\"type\":\"banana\"}"), Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Update_builds_proposal_with_current_state_hash()
    {
        var categoryId = Guid.NewGuid();
        var current = new CategoryListItemDto(categoryId, "Mercado", CategoryType.Expense, "#ffffff", true);
        var (repo, factory) = NewFactory();
        var sender = new MapSender().On<ListCategoriesQuery>(_ => (IReadOnlyCollection<CategoryListItemDto>)new[] { current });
        var tool = new ProposeCategoryUpdateTool(sender, factory);

        var result = await tool.ExecuteAsync(
            Args($"{{\"categoryId\":\"{categoryId}\",\"name\":\"Supermercado\"}}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var proposal = Assert.Single(repo.Proposals);
        Assert.Equal(AiActionTypes.CategoryUpdate, proposal.ActionType);
        Assert.Equal(ProposalState.CategoryHash("Mercado", CategoryType.Expense, "#ffffff", true), proposal.StateHash);
    }

    [Fact]
    public async Task Archive_refuses_an_already_archived_category()
    {
        var categoryId = Guid.NewGuid();
        var archived = new CategoryListItemDto(categoryId, "Antiga", CategoryType.Expense, null, false);
        var (repo, factory) = NewFactory();
        var sender = new MapSender().On<ListCategoriesQuery>(_ => (IReadOnlyCollection<CategoryListItemDto>)new[] { archived });
        var tool = new ProposeCategoryArchiveTool(sender, factory);

        var result = await tool.ExecuteAsync(Args($"{{\"categoryId\":\"{categoryId}\"}}"), Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(repo.Proposals);
    }
}
