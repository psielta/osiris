using System.Text.Json;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Tools.Read;
using Osiris.Application.Features.Bills.DTOs;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Application.Features.FinancialAccounts.DTOs;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class ReadToolsTests
{
    private static AiAgentContext Context() =>
        new(Guid.NewGuid(), "user-1", Guid.NewGuid(), "corr", new DateOnly(2026, 6, 22), false);

    private static JsonElement Args(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task ListFinancialAccounts_sumsActiveBalances_andEmitsSources()
    {
        IReadOnlyCollection<FinancialAccountListItemDto> accounts = new[]
        {
            new FinancialAccountListItemDto(Guid.NewGuid(), "Conta Corrente", FinancialAccountType.CheckingAccount, 1000m, true),
            new FinancialAccountListItemDto(Guid.NewGuid(), "Poupança", FinancialAccountType.SavingsAccount, 500m, true),
            new FinancialAccountListItemDto(Guid.NewGuid(), "Antiga", FinancialAccountType.Other, 999m, false)
        };
        var tool = new ListFinancialAccountsTool(new FakeSender(accounts));

        var result = await tool.ExecuteAsync(Args("{}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AiToolRisk.ReadOnly, tool.Risk);
        using var document = JsonDocument.Parse(result.ResultJson);
        Assert.Equal(1500m, document.RootElement.GetProperty("totalBalance").GetDecimal());
        Assert.NotNull(result.Sources);
        Assert.Equal(3, result.Sources!.Count);
    }

    [Fact]
    public async Task ListBills_filtersByStatus_andComputesOpenTotal()
    {
        var categoryId = Guid.NewGuid();
        IReadOnlyCollection<BillListItemDto> bills = new[]
        {
            new BillListItemDto(Guid.NewGuid(), "Aluguel", 1200m, new DateOnly(2026, 6, 10), null, BillStatus.Pending, categoryId, "Moradia", null, null, null),
            new BillListItemDto(Guid.NewGuid(), "Luz", 200m, new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 4), BillStatus.Paid, categoryId, "Casa", null, null, null)
        };
        var tool = new ListBillsTool(new FakeSender(bills));

        var result = await tool.ExecuteAsync(Args("{\"status\":\"Pending\"}"), Context(), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ResultJson);
        Assert.Equal(1, document.RootElement.GetProperty("billCount").GetInt32());
        Assert.Equal(1200m, document.RootElement.GetProperty("openTotal").GetDecimal());
    }

    [Fact]
    public async Task GetStatementsOverview_byDefault_keepsOnlyOpen_andTotalsOpenBalance()
    {
        IReadOnlyCollection<CreditCardStatementOverviewDto> statements = new[]
        {
            new CreditCardStatementOverviewDto(Guid.NewGuid(), Guid.NewGuid(), "Inter", 6, 2026,
                new DateOnly(2026, 6, 26), new DateOnly(2026, 7, 13), CreditCardStatementStatus.Open, 800m, 0m, 800m),
            new CreditCardStatementOverviewDto(Guid.NewGuid(), Guid.NewGuid(), "BB", 6, 2026,
                new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 9), CreditCardStatementStatus.PartiallyPaid, 500m, 200m, 300m),
            new CreditCardStatementOverviewDto(Guid.NewGuid(), Guid.NewGuid(), "Nubank", 5, 2026,
                new DateOnly(2026, 5, 26), new DateOnly(2026, 6, 13), CreditCardStatementStatus.Paid, 400m, 400m, 0m)
        };
        var tool = new GetStatementsOverviewTool(new FakeSender(statements));

        var result = await tool.ExecuteAsync(Args("{}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        using var document = JsonDocument.Parse(result.ResultJson);
        Assert.Equal(2, document.RootElement.GetProperty("statementCount").GetInt32());
        Assert.Equal(1100m, document.RootElement.GetProperty("totalOpenBalance").GetDecimal());
        Assert.Equal(2, result.Sources!.Count);
    }

    [Fact]
    public async Task GetStatementsOverview_withOnlyOpenFalse_keepsAllStatements()
    {
        IReadOnlyCollection<CreditCardStatementOverviewDto> statements = new[]
        {
            new CreditCardStatementOverviewDto(Guid.NewGuid(), Guid.NewGuid(), "Inter", 6, 2026,
                new DateOnly(2026, 6, 26), new DateOnly(2026, 7, 13), CreditCardStatementStatus.Open, 800m, 0m, 800m),
            new CreditCardStatementOverviewDto(Guid.NewGuid(), Guid.NewGuid(), "Nubank", 5, 2026,
                new DateOnly(2026, 5, 26), new DateOnly(2026, 6, 13), CreditCardStatementStatus.Paid, 400m, 400m, 0m)
        };
        var tool = new GetStatementsOverviewTool(new FakeSender(statements));

        var result = await tool.ExecuteAsync(Args("{\"onlyOpen\":false}"), Context(), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ResultJson);
        Assert.Equal(2, document.RootElement.GetProperty("statementCount").GetInt32());
    }

    [Fact]
    public async Task GetAccountStatement_withInvalidId_fails()
    {
        var tool = new GetAccountStatementTool(new FakeSender(new object()));

        var result = await tool.ExecuteAsync(Args("{\"accountId\":\"not-a-guid\"}"), Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetFinancialDefinition_knownTerm_isFound()
    {
        var tool = new GetFinancialDefinitionTool();

        var result = await tool.ExecuteAsync(Args("{\"term\":\"fatura\"}"), Context(), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ResultJson);
        Assert.True(document.RootElement.GetProperty("found").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("definition").GetString()));
    }

    [Fact]
    public async Task GetFinancialDefinition_unknownTerm_isNotFound()
    {
        var tool = new GetFinancialDefinitionTool();

        var result = await tool.ExecuteAsync(Args("{\"term\":\"bitcoin\"}"), Context(), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ResultJson);
        Assert.False(document.RootElement.GetProperty("found").GetBoolean());
    }
}
