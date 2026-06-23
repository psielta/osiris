using System.Text.Json;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.AiAssistant.Tools.Proposals;
using Osiris.Application.Features.FinancialAccounts.DTOs;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class AccountProposalToolsTests
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
    public async Task Creation_creates_a_pending_proposal_with_default_balance()
    {
        var (repo, factory) = NewFactory();
        var tool = new ProposeAccountCreationTool(factory);

        var result = await tool.ExecuteAsync(Args("{\"name\":\"Nubank\",\"type\":\"checking\"}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var proposal = Assert.Single(repo.Proposals);
        Assert.Equal(AiActionTypes.AccountCreation, proposal.ActionType);
    }

    [Fact]
    public async Task Update_uses_current_profile_hash_excluding_balance()
    {
        var accountId = Guid.NewGuid();
        var current = new FinancialAccountListItemDto(accountId, "Conta", FinancialAccountType.CheckingAccount, 999m, true);
        var (repo, factory) = NewFactory();
        var sender = new MapSender().On<ListFinancialAccountsQuery>(_ => (IReadOnlyCollection<FinancialAccountListItemDto>)new[] { current });
        var tool = new ProposeAccountUpdateTool(sender, factory);

        var result = await tool.ExecuteAsync(Args($"{{\"accountId\":\"{accountId}\",\"name\":\"Conta Nova\"}}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var proposal = Assert.Single(repo.Proposals);
        Assert.Equal(ProposalState.AccountProfileHash("Conta", FinancialAccountType.CheckingAccount, true), proposal.StateHash);
    }

    [Fact]
    public async Task Archive_refuses_an_already_archived_account()
    {
        var accountId = Guid.NewGuid();
        var archived = new FinancialAccountListItemDto(accountId, "Antiga", FinancialAccountType.Other, 0m, false);
        var (repo, factory) = NewFactory();
        var sender = new MapSender().On<ListFinancialAccountsQuery>(_ => (IReadOnlyCollection<FinancialAccountListItemDto>)new[] { archived });
        var tool = new ProposeAccountArchiveTool(sender, factory);

        var result = await tool.ExecuteAsync(Args($"{{\"accountId\":\"{accountId}\"}}"), Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(repo.Proposals);
    }
}
