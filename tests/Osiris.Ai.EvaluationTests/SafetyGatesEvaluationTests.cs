using System.Text.Json;
using Osiris.Ai.EvaluationTests.Support;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.AiAssistant.Tools.Read;
using Osiris.Domain.Enums;

namespace Osiris.Ai.EvaluationTests;

/// <summary>
/// Static safety gates over the real read-tool catalogue: read-only only, no tenant/user in schemas,
/// no write/dangerous tool names, and the execution policy denies anything but reads while writes are off.
/// </summary>
public sealed class SafetyGatesEvaluationTests
{
    private static IReadOnlyList<IAiTool> RealReadTools()
    {
        var sender = new ThrowingSender();
        return new IAiTool[]
        {
            new GetFinancialSnapshotTool(sender),
            new ListFinancialAccountsTool(sender),
            new GetAccountStatementTool(sender),
            new SearchAccountMovementsTool(sender),
            new GetSpendingSummaryTool(sender),
            new GetCashFlowSummaryTool(sender),
            new ListCreditCardsTool(sender),
            new GetCardStatementTool(sender),
            new SearchCardPurchasesTool(sender),
            new ListBillsTool(sender),
            new GetUpcomingObligationsTool(sender),
            new ListCategoriesTool(sender),
            new GetFinancialDefinitionTool()
        };
    }

    [Fact]
    public void Every_catalogued_tool_is_read_only()
    {
        Assert.All(RealReadTools(), tool => Assert.Equal(AiToolRisk.ReadOnly, tool.Risk));
    }

    [Fact]
    public void No_tool_schema_accepts_tenant_or_user_identifiers()
    {
        foreach (var tool in RealReadTools())
        {
            var schema = JsonSerializer.Serialize(tool.InputSchema).ToLowerInvariant();
            Assert.DoesNotContain("tenantid", schema);
            Assert.DoesNotContain("userid", schema);
        }
    }

    [Fact]
    public void Catalogue_has_no_write_or_dangerous_tool_names()
    {
        var dangerousFragments = new[] { "propose", "delete", "sql", "shell", "exec", "http", "secret" };
        foreach (var tool in RealReadTools())
        {
            Assert.DoesNotContain(
                dangerousFragments,
                fragment => tool.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData(AiToolRisk.WriteProposal)]
    [InlineData(AiToolRisk.Restricted)]
    [InlineData(AiToolRisk.Forbidden)]
    public void Policy_denies_non_read_tools_while_writes_are_disabled(AiToolRisk risk)
    {
        var policy = new AiToolExecutionPolicy();

        var decision = policy.Evaluate(EvaluationHarness.Context(writesEnabled: false), new RecordingTool("x", risk));

        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public void Policy_allows_read_tools()
    {
        var policy = new AiToolExecutionPolicy();

        var decision = policy.Evaluate(EvaluationHarness.Context(), new RecordingTool("read", AiToolRisk.ReadOnly));

        Assert.True(decision.IsAllowed);
    }
}
