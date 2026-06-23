using System.Text.Json;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Tools.Read;
using Osiris.Application.Features.Bills.DTOs;
using Osiris.Application.Features.CreditCardPurchases.DTOs;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
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
    public async Task GetCardPurchaseDetails_returnsInstallmentPlan()
    {
        var installments = new[]
        {
            new CreditCardPurchaseInstallmentDto(Guid.NewGuid(), 1, 3, 100m, new DateOnly(2026, 7, 7), Guid.NewGuid(), 7, 2026),
            new CreditCardPurchaseInstallmentDto(Guid.NewGuid(), 2, 3, 100m, new DateOnly(2026, 8, 7), Guid.NewGuid(), 8, 2026),
            new CreditCardPurchaseInstallmentDto(Guid.NewGuid(), 3, 3, 100m, new DateOnly(2026, 9, 7), Guid.NewGuid(), 9, 2026)
        };
        var details = new CreditCardPurchaseDetailsDto(
            Guid.NewGuid(), Guid.NewGuid(), "Inter", "Eletrônicos", Guid.NewGuid(), "Notebook", 300m,
            new DateOnly(2026, 6, 18), 3, null, installments);
        var tool = new GetCardPurchaseDetailsTool(new FakeSender(details));

        var result = await tool.ExecuteAsync(Args($"{{\"creditCardPurchaseId\":\"{details.Id}\"}}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        using var document = JsonDocument.Parse(result.ResultJson);
        Assert.Equal(3, document.RootElement.GetProperty("purchase").GetProperty("installmentItems").GetArrayLength());
        Assert.Single(result.Sources!);
    }

    [Fact]
    public async Task GetCardStatementDetails_returnsItemsAndPayments()
    {
        var items = new[]
        {
            new CreditCardStatementInstallmentItemDto(Guid.NewGuid(), Guid.NewGuid(), "Notebook", 1, 3, 100m),
            new CreditCardStatementInstallmentItemDto(Guid.NewGuid(), Guid.NewGuid(), "Mercado", 1, 1, 50m)
        };
        var payments = new[]
        {
            new CreditCardStatementPaymentItemDto(Guid.NewGuid(), 80m, new DateOnly(2026, 7, 9), Guid.NewGuid(), "Conta", null)
        };
        var details = new CreditCardStatementDetailsDto(
            Guid.NewGuid(), Guid.NewGuid(), "Inter", 7, 2026, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 7),
            CreditCardStatementStatus.Open, 150m, 80m, 70m, items, payments);
        var tool = new GetCardStatementDetailsTool(new FakeSender(details));

        var result = await tool.ExecuteAsync(Args($"{{\"statementId\":\"{details.Id}\"}}"), Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        using var document = JsonDocument.Parse(result.ResultJson);
        var statement = document.RootElement.GetProperty("statement");
        Assert.Equal(2, statement.GetProperty("items").GetArrayLength());
        Assert.Equal(1, statement.GetProperty("payments").GetArrayLength());
    }

    [Fact]
    public async Task SearchAccountMovements_withOnlyUncategorized_keepsMovementsWithoutCategory()
    {
        var categoryId = Guid.NewGuid();
        IReadOnlyCollection<MovementListItemDto> movements = new[]
        {
            new MovementListItemDto(Guid.NewGuid(), FinancialAccountMovementType.Expense, 50m, false, new DateOnly(2026, 6, 10), "Sem categoria", null, null),
            new MovementListItemDto(Guid.NewGuid(), FinancialAccountMovementType.Expense, 80m, false, new DateOnly(2026, 6, 12), "Com categoria", categoryId, null)
        };
        var tool = new SearchAccountMovementsTool(new FakeSender(movements));

        var result = await tool.ExecuteAsync(
            Args($"{{\"accountId\":\"{Guid.NewGuid()}\",\"onlyUncategorized\":true}}"), Context(), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ResultJson);
        Assert.Equal(1, document.RootElement.GetProperty("movementCount").GetInt32());
        Assert.False(document.RootElement.GetProperty("movements")[0].GetProperty("hasCategory").GetBoolean());
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
