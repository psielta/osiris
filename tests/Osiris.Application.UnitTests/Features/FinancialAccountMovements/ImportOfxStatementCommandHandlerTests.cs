using Osiris.Application.Features.FinancialAccountMovements.Commands.ImportOfxStatement;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class ImportOfxStatementCommandHandlerTests
{
    private static readonly DateOnly Date = new(2026, 6, 2);

    [Fact]
    public async Task Handle_ImportsLinesUpdatesBalanceAndSetsExternalId()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null),
            new("A2", Date, 90m, FinancialAccountMovementType.Expense, "Mercado", null),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Imported);
        Assert.Equal(0, result.Value.SkippedDuplicates);
        Assert.Equal(1510m, account.CurrentBalance);
        Assert.Equal(2, context.Movements.Movements.Count);
        Assert.Contains(context.Movements.Movements, movement => movement.ExternalId == "A1");
    }

    [Fact]
    public async Task Handle_SkipsDuplicatesAcrossReimport()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null),
        };

        await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);
        var second = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.Equal(0, second.Value!.Imported);
        Assert.Equal(1, second.Value.SkippedDuplicates);
        Assert.Single(context.Movements.Movements);
        Assert.Equal(1500m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_SkipsDuplicatesWithinSameBatch()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var lines = new List<ImportOfxLineInput>
        {
            new("DUP", Date, 10m, FinancialAccountMovementType.Income, "A", null),
            new("DUP", Date, 10m, FinancialAccountMovementType.Income, "A", null),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.Equal(1, result.Value!.Imported);
        Assert.Equal(1, result.Value.SkippedDuplicates);
        Assert.Single(context.Movements.Movements);
    }

    [Fact]
    public async Task Handle_WhenCategoryInvalid_Fails()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 10m, FinancialAccountMovementType.Income, "A", Guid.NewGuid()),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(context.Movements.Movements);
    }

    [Fact]
    public async Task Handle_AppliesProvidedCategory()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);
        var category = new FinancialCategory(tenantId, "Salário", CategoryType.Income);
        context.Categories.Add(category);

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", category.Id),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(category.Id, context.Movements.Movements.Single().CategoryId);
    }

    private sealed class HandlerContext
    {
        public HandlerContext(Guid tenantId)
        {
            Accounts = new FakeFinancialAccountRepository();
            Movements = new FakeFinancialAccountMovementRepository();
            Categories = new FakeCategoryRepository();
            Handler = new ImportOfxStatementCommandHandler(
                Accounts,
                Movements,
                Categories,
                new FakeCurrentUser(tenantId),
                new FakeDateTimeProvider());
        }

        public FakeFinancialAccountRepository Accounts { get; }

        public FakeFinancialAccountMovementRepository Movements { get; }

        public FakeCategoryRepository Categories { get; }

        public ImportOfxStatementCommandHandler Handler { get; }
    }
}
